#!/usr/bin/env python3
"""Keep verified Nightly history in one Archived release."""
import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile
from zoneinfo import ZoneInfo

ARCHIVED = 'Archived'
INDEX = ('ARCHIVE.json', 'SHA256SUMS-ARCHIVE.txt')
DAILY_TAG = re.compile(r'archive-\d{4}-\d{2}-\d{2}')
LEGACY_TAG = re.compile(r'archive-\d{8}-\d{6}-[a-f0-9]{12}')
ASSET_LIMIT = 1000


def gh(*args):
    return subprocess.check_output(['gh', *args])


def api(repo, path):
    return json.loads(gh('api', f'repos/{repo}/{path}'))


def digest(path):
    value = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            value.update(chunk)
    return value.hexdigest()


def releases(repo):
    return [release for page in json.loads(gh('api', f'repos/{repo}/releases?per_page=100', '--paginate', '--slurp')) for release in page]


def release_assets(repo, release):
    pages = json.loads(gh('api', f'repos/{repo}/releases/{release["id"]}/assets?per_page=100',
                          '--paginate', '--slurp'))
    return [asset for page in pages for asset in page]


def source_commit(repo, tag):
    ref = api(repo, 'git/ref/tags/' + tag)['object']
    while ref['type'] == 'tag':
        ref = api(repo, 'git/tags/' + ref['sha'])['object']
    return ref['sha']


def build_stamp(release):
    old = re.fullmatch(r'archive-(\d{8})-(\d{6})-([a-f0-9]{12})', release['tag_name'])
    if old:
        stamp = datetime.strptime(old[1] + old[2], '%Y%m%d%H%M%S').replace(tzinfo=timezone.utc)
        identity = old[3]
    else:
        assets = release['assets']
        stamp = datetime.fromisoformat(max(a['updated_at'] for a in assets).replace('Z', '+00:00'))
        identity = hashlib.sha256(json.dumps([(a['id'], a['size'], a['updated_at']) for a in sorted(assets, key=lambda a: a['name'])], sort_keys=True).encode()).hexdigest()[:12]
    return stamp, identity


def grouping(stamp, identity, zone=None):
    local = stamp.astimezone(zone or ZoneInfo('America/Chicago'))
    # UTC prefix disambiguates repeated local times when daylight saving ends.
    return ARCHIVED, stamp.astimezone(timezone.utc).strftime('%Y%m%d-%H%M%SZ') + '-' + identity, local.isoformat()


def validate_names(assets):
    names = [a['name'] for a in assets]
    if len(set(names)) != len(names) or any(not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9._-]*', n) or n in ('.', '..') for n in names):
        raise ValueError('Unsafe or duplicate release asset name')
    return names


def download(repo, tag, directory, names=None):
    args = ['release', 'download', tag, '--repo', repo, '--dir', str(directory)]
    for name in names or []:
        args.extend(['--pattern', name])
    gh(*args)


def notes(repo, manifest):
    lines = ['Archived Nightly builds, newest first.', '',
             'Download the files with the same timestamp/build suffix. ARCHIVE.json retains original filenames, source revisions, release notes and SHA-256 hashes. SHA256SUMS-ARCHIVE.txt checks the renamed downloads.', '',
             '| Build time (America/Chicago) | Source | Downloads |', '|---|---|---|']
    for record in sorted(manifest['builds'].values(), key=lambda b: b['timestamp'], reverse=True):
        links = ' · '.join(f'[{a["originalName"]}](https://github.com/{repo}/releases/download/{manifest["tag"]}/{a["name"]})' for a in record['assets'])
        source = record['source']
        lines.append(f'| {record["localTime"]} | [{source[:12]}](https://github.com/{repo}/commit/{source}) | {links} |')
    return '\n'.join(lines) + '\n'


def sums_text(manifest):
    return ''.join(asset['sha256'] + '  ' + asset['name'] + '\n'
                   for record in manifest['builds'].values() for asset in record['assets'])


def read_index(path, tag):
    manifest = json.loads((path / INDEX[0]).read_text())
    if (manifest.get('schema') != 1 or manifest.get('tag') != tag or
            manifest.get('timezone') != 'America/Chicago' or not isinstance(manifest.get('builds'), dict)):
        raise ValueError('Invalid archive manifest')
    assets = []
    for record in manifest['builds'].values():
        if not re.fullmatch(r'[a-f0-9]{40}', record['source']) or not record['assets']:
            raise ValueError('Invalid archived build')
        for asset in record['assets']:
            if (not re.fullmatch(r'[a-f0-9]{64}', asset['sha256']) or type(asset['size']) is not int or
                    asset['size'] < 0):
                raise ValueError('Invalid archived asset')
            validate_names([{'name': asset['originalName']}])
            assets.append(asset)
    validate_names(assets)
    if (path / INDEX[1]).read_text() != sums_text(manifest):
        raise ValueError('Archive manifest and checksums disagree')
    return manifest


def verified_download(repo, release, directory):
    assets = release['assets']
    validate_names(assets)
    download(repo, release['tag_name'], directory)
    for asset in assets:
        path = directory / asset['name']
        if not path.is_file() or path.stat().st_size != asset['size']:
            raise ValueError('Incomplete archive download')


def raw_build(repo, release, old):
    stamp, identity = build_stamp(release)
    _, key, local = grouping(stamp, identity)
    source = re.search(r'Original build source: `([a-f0-9]{40})`', release.get('body') or '')
    source = source[1] if source else source_commit(repo, release['tag_name'])
    record = {'timestamp': stamp.isoformat(), 'localTime': local, 'source': source,
              'originalTag': release['tag_name'], 'releaseNotes': release.get('body') or '', 'assets': []}
    files = {}
    for asset in release['assets']:
        original = asset['name']
        name = key + '--' + original
        record['assets'].append({'name': name, 'originalName': original, 'size': asset['size'],
                                 'sha256': digest(old / original)})
        files[name] = old / original
    return {key: record}, files


def daily_builds(release, old):
    manifest = read_index(old, release['tag_name'])
    recorded = {asset['name'] for record in manifest['builds'].values() for asset in record['assets']}
    if recorded != {asset['name'] for asset in release['assets']} - set(INDEX) or not recorded:
        raise ValueError('Daily archive has unindexed or missing assets')
    records, files = {}, {}
    for key, record in manifest['builds'].items():
        if not re.fullmatch(r'\d{6}Z-[a-f0-9]{12}', key):
            raise ValueError('Invalid daily archive build identifier')
        stamp = datetime.fromisoformat(record['timestamp'])
        _, target, _ = grouping(stamp, key.rsplit('-', 1)[1])
        new_assets = []
        for asset in record['assets']:
            original = old / asset['name']
            if original.stat().st_size != asset['size'] or digest(original) != asset['sha256']:
                raise ValueError('Daily archive does not match its recorded checksum')
            name = target + '--' + asset['originalName']
            files[name] = original
            new_assets.append(dict(asset, name=name))
        validate_names(new_assets)
        if target in records:
            raise ValueError('Duplicate build identifier in daily archive')
        records[target] = dict(record, assets=new_assets)
    return records, files


def store(repo, root, records, files):
    existing = next((item for item in releases(repo) if item['tag_name'] == ARCHIVED), None)
    manifest = {'schema': 1, 'tag': ARCHIVED, 'timezone': 'America/Chicago', 'builds': {}}
    uploaded = set()
    if existing:
        uploaded = set(validate_names(release_assets(repo, existing)))
        if set(INDEX) <= uploaded:
            metadata = root / 'metadata'; metadata.mkdir()
            download(repo, ARCHIVED, metadata, INDEX)
            manifest = read_index(metadata, ARCHIVED)
        elif not existing.get('draft'):
            raise ValueError('Published archive lacks its manifest and checksums')

    build_index = manifest['builds']
    for key, record in records.items():
        if key in build_index and build_index[key] != record:
            raise ValueError('Conflicting archived build; refusing to replace preserved downloads')
        build_index[key] = record
    validate_names([asset for build in build_index.values() for asset in build['assets']])
    if len(uploaded | set(files) | set(INDEX)) > ASSET_LIMIT:
        raise ValueError('Archived would exceed the GitHub release asset limit')

    if existing is None:
        gh('release', 'create', ARCHIVED, '--repo', repo, '--target',
           os.environ.get('SOURCE_SHA') or api(repo, 'git/ref/heads/master')['object']['sha'],
           '--draft', '--prerelease', '--title', ARCHIVED, '--notes', 'Archive preparation in progress.')
    upload = root / 'upload'; upload.mkdir()
    for name, source in files.items():
        if name not in uploaded:
            path = upload / name
            source.rename(path)
            gh('release', 'upload', ARCHIVED, str(path), '--repo', repo)
    # Never replace existing binaries. Verify even assets left by a previous attempt.
    verified = root / 'verified'; verified.mkdir()
    download(repo, ARCHIVED, verified, list(files))
    if any(not (verified / asset['name']).is_file() or digest(verified / asset['name']) != asset['sha256']
           for record in records.values() for asset in record['assets']):
        raise ValueError('Archive verification failed; original release is unchanged')

    metadata_path = root / INDEX[0]
    metadata_path.write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    sums = root / INDEX[1]
    sums.write_text(sums_text(manifest), encoding='utf-8')
    gh('release', 'upload', ARCHIVED, str(metadata_path), str(sums), '--repo', repo, '--clobber')
    check = root / 'check'; check.mkdir()
    download(repo, ARCHIVED, check, INDEX)
    if any(digest(check / name) != digest(root / name) for name in INDEX):
        raise ValueError('Archive index verification failed; original release is unchanged')
    body = root / 'notes.md'
    body.write_text(notes(repo, manifest), encoding='utf-8')
    gh('release', 'edit', ARCHIVED, '--repo', repo, '--draft=false', '--prerelease',
       '--title', ARCHIVED, '--notes-file', str(body))


def preserve(repo, release, incoming=None):
    if not release['assets']:
        return False
    with tempfile.TemporaryDirectory(prefix='chummer-archive-') as temporary:
        root = Path(temporary)
        old = root / 'old'; old.mkdir()
        verified_download(repo, release, old)
        names = {asset['name'] for asset in release['assets']}
        if (incoming is not None and names == {p.name for p in incoming.iterdir() if p.is_file()} and
                all(digest(old / name) == digest(incoming / name) for name in names)):
            return False
        records, files = (daily_builds(release, old) if DAILY_TAG.fullmatch(release['tag_name'])
                          else raw_build(repo, release, old))
        store(repo, root, records, files)
        print('Preserved ' + release['tag_name'] + ' in ' + ARCHIVED, flush=True)
        return True


def archive(repo, incoming):
    nightly = next((release for release in releases(repo) if release['tag_name'] == 'Nightly'), None)
    if nightly:
        preserve(repo, nightly, incoming)


def consolidate(repo, retire_sources=False):
    retired = []
    for release in sorted(releases(repo), key=lambda item: item['tag_name']):
        tag = release['tag_name']
        if (DAILY_TAG.fullmatch(tag) or LEGACY_TAG.fullmatch(tag)) and preserve(repo, release):
            retired.append(tag)
    if retire_sources:
        for tag in retired:
            # Keep original Git tags. Every source was verified before removing any release.
            gh('release', 'delete', tag, '--repo', repo, '--yes')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('incoming', nargs='?', type=Path)
    parser.add_argument('--consolidate', action='store_true')
    parser.add_argument('--retire-source-releases', action='store_true')
    args = parser.parse_args()
    if args.retire_source_releases and not args.consolidate:
        parser.error('--retire-source-releases requires --consolidate')
    if args.consolidate:
        consolidate(os.environ['GH_REPO'], args.retire_source_releases)
    elif args.incoming:
        archive(os.environ['GH_REPO'], args.incoming)
    else:
        parser.error('supply an incoming asset directory or --consolidate')
