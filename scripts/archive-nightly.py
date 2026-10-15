#!/usr/bin/env python3
"""Keep verified Nightly history in one release per America/Chicago calendar day."""
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
    return 'archive-' + local.strftime('%Y-%m-%d'), stamp.strftime('%H%M%SZ') + '-' + identity, local.isoformat()


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
    lines = ['Archived Nightly builds grouped by America/Chicago calendar day.', '',
             'Download the files with the same timestamp/build suffix. ARCHIVE.json retains original filenames, source revisions, release notes and SHA-256 hashes. SHA256SUMS-ARCHIVE.txt checks the renamed downloads.', '',
             '| Build time (America/Chicago) | Source | Downloads |', '|---|---|---|']
    for record in sorted(manifest['builds'].values(), key=lambda b: b['timestamp'], reverse=True):
        links = ' · '.join(f'[{a["originalName"]}](https://github.com/{repo}/releases/download/{manifest["tag"]}/{a["name"]})' for a in record['assets'])
        source = record['source']
        lines.append(f'| {record["localTime"]} | [{source[:12]}](https://github.com/{repo}/commit/{source}) | {links} |')
    return '\n'.join(lines) + '\n'


def preserve(repo, release, incoming=None):
    assets = release['assets']
    if not assets:
        return None
    names = validate_names(assets)
    stamp, identity = build_stamp(release)
    tag, key, local = grouping(stamp, identity)
    with tempfile.TemporaryDirectory(prefix='chummer-archive-') as temporary:
        root = Path(temporary)
        old = root / 'old'; old.mkdir()
        download(repo, release['tag_name'], old)
        for asset in assets:
            path = old / asset['name']
            if not path.is_file() or path.stat().st_size != asset['size']:
                raise ValueError('Incomplete archive download')
        if incoming is not None and set(names) == {p.name for p in incoming.iterdir() if p.is_file()} and all(digest(old / n) == digest(incoming / n) for n in names):
            return None
        source = re.search(r'Original build source: `([a-f0-9]{40})`', release.get('body') or '')
        source = source[1] if source else source_commit(repo, release['tag_name'])
        existing = next((r for r in releases(repo) if r['tag_name'] == tag), None)
        manifest = {'schema': 1, 'tag': tag, 'timezone': 'America/Chicago', 'builds': {}}
        if existing:
            has_manifest = any(a['name'] == 'ARCHIVE.json' for a in existing['assets'])
            if not has_manifest and not existing.get('draft'):
                raise ValueError('Daily archive exists without its manifest; refusing to overwrite')
            if has_manifest:
                metadata = root / 'metadata'; metadata.mkdir()
                download(repo, tag, metadata, ['ARCHIVE.json'])
                manifest = json.loads((metadata / 'ARCHIVE.json').read_text())
            if manifest.get('schema') != 1 or manifest.get('tag') != tag or manifest.get('timezone') != 'America/Chicago' or not isinstance(manifest.get('builds'), dict):
                raise ValueError('Invalid daily archive manifest')
        record = {'timestamp': stamp.isoformat(), 'localTime': local, 'source': source,
                  'originalTag': release['tag_name'], 'releaseNotes': release.get('body') or '', 'assets': []}
        upload = root / 'upload'; upload.mkdir()
        for asset in assets:
            name = key + '--' + asset['name']
            path = upload / name
            (old / asset['name']).rename(path)
            record['assets'].append({'name': name, 'originalName': asset['name'], 'size': asset['size'], 'sha256': digest(path)})
        previous = manifest['builds'].get(key)
        if previous and (previous['source'] != source or previous['assets'] != record['assets']):
            raise ValueError('Conflicting archived build; refusing to replace preserved downloads')
        if existing is None:
            gh('release', 'create', tag, '--repo', repo, '--target', os.environ.get('SOURCE_SHA') or api(repo, 'git/ref/heads/master')['object']['sha'], '--draft', '--prerelease', '--title', 'Archive — ' + tag.removeprefix('archive-'), '--notes', 'Archive preparation in progress.')
        # Uploading a conflicting asset fails. A retry may reuse only already matching bytes.
        uploaded = {a['name'] for a in existing['assets']} if existing else set()
        for asset in record['assets']:
            if asset['name'] not in uploaded:
                gh('release', 'upload', tag, str(upload / asset['name']), '--repo', repo)
        verified = root / 'verified'; verified.mkdir()
        download(repo, tag, verified, [a['name'] for a in record['assets']])
        if any(not (verified / a['name']).is_file() or digest(verified / a['name']) != a['sha256'] for a in record['assets']):
            raise ValueError('Archive verification failed; original release is unchanged')
        manifest['builds'][key] = previous or record
        metadata_path = root / 'ARCHIVE.json'; metadata_path.write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
        sums = root / 'SHA256SUMS-ARCHIVE.txt'
        sums.write_text(''.join(a['sha256'] + '  ' + a['name'] + '\n' for b in manifest['builds'].values() for a in b['assets']), encoding='utf-8')
        gh('release', 'upload', tag, str(metadata_path), str(sums), '--repo', repo, '--clobber')
        check = root / 'check'; check.mkdir()
        download(repo, tag, check, ['ARCHIVE.json', 'SHA256SUMS-ARCHIVE.txt'])
        if digest(check / 'ARCHIVE.json') != digest(metadata_path) or digest(check / sums.name) != digest(sums):
            raise ValueError('Archive metadata verification failed; original release is unchanged')
        body = root / 'notes.md'; body.write_text(notes(repo, manifest), encoding='utf-8')
        gh('release', 'edit', tag, '--repo', repo, '--draft=false', '--prerelease', '--title', 'Archive — ' + tag.removeprefix('archive-'), '--notes-file', str(body))
        print('Preserved ' + release['tag_name'] + ' in ' + tag)
        return tag


def archive(repo, incoming):
    nightly = next((r for r in releases(repo) if r['tag_name'] == 'Nightly'), None)
    if nightly:
        preserve(repo, nightly, incoming)


def consolidate(repo):
    for release in sorted(releases(repo), key=lambda r: r['tag_name']):
        if re.fullmatch(r'archive-\d{8}-\d{6}-[a-f0-9]{12}', release['tag_name']):
            if preserve(repo, release):
                # Keep old Git tags; only remove duplicated release headings after verification.
                gh('release', 'delete', release['tag_name'], '--repo', repo, '--yes')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('incoming', nargs='?', type=Path)
    parser.add_argument('--consolidate', action='store_true')
    args = parser.parse_args()
    if args.consolidate:
        consolidate(os.environ['GH_REPO'])
    elif args.incoming:
        archive(os.environ['GH_REPO'], args.incoming)
    else:
        parser.error('supply an incoming asset directory or --consolidate')
