#!/usr/bin/env python3
"""Verify the reviewed offline PDF.js bundle; optionally detect upstream updates.

The manifest is a review record, not a signature. Updating it requires reviewing
and testing a new official package, never automatically blessing changed files.
"""
import argparse
import hashlib
import json
import pathlib
import re
import sys
import urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[1]
MANIFEST = ROOT / 'scripts/pdfjs-manifest.json'
REGISTRY = 'https://registry.npmjs.org/pdfjs-dist/'


def verify(bundle, manifest):
    version = manifest['version']
    if not re.fullmatch(r'\d+\.\d+\.\d+', version):
        raise ValueError('Invalid PDF.js version')
    if manifest['tarball'] != REGISTRY + '-/pdfjs-dist-' + version + '.tgz':
        raise ValueError('PDF.js must come from the official npm registry')
    if not re.fullmatch(r'sha512-[A-Za-z0-9+/]{86}==', manifest['integrity']):
        raise ValueError('Missing official package integrity')
    actual = {}
    for path in bundle.rglob('*'):
        if path.is_symlink():
            raise ValueError('PDF.js bundle contains a symbolic link')
        if path.is_file():
            actual[path.relative_to(bundle).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
    expected = manifest['files']
    if not expected or actual != expected:
        changed = sorted(set(actual) ^ set(expected) | {p for p in actual.keys() & expected.keys() if actual[p] != expected[p]})
        raise ValueError('PDF.js bundle differs from reviewed manifest: ' + ', '.join(changed))
    provenance = (bundle / 'VERSION.txt').read_text(encoding='utf-8')
    for value in ('pdfjs-dist ' + version, manifest['tarball'], manifest['integrity']):
        if value not in provenance:
            raise ValueError('PDF.js provenance and manifest disagree')


def get_metadata(url):
    request = urllib.request.Request(url, headers={'Accept': 'application/json', 'User-Agent': 'Chummer-PDFjs-audit'})
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)


def check_upstream(manifest, fetch=get_metadata):
    pinned = fetch(REGISTRY + manifest['version'])
    if pinned['version'] != manifest['version'] or pinned['dist']['integrity'] != manifest['integrity'] or pinned['dist']['tarball'] != manifest['tarball']:
        raise ValueError('Official PDF.js package metadata differs from the reviewed provenance')
    latest = fetch(REGISTRY + 'latest')['version']
    if not isinstance(latest, str) or not re.fullmatch(r'\d+\.\d+\.\d+', latest):
        raise ValueError('Invalid latest PDF.js metadata')
    if latest != manifest['version']:
        raise ValueError(f'PDF.js {latest} is available; review the changes and test the reader before replacing {manifest["version"]}')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--upstream', action='store_true', help='also check official metadata; requires network access')
    args = parser.parse_args()
    manifest = json.loads(MANIFEST.read_text(encoding='utf-8'))
    verify(ROOT / 'Chummer.Mac/pdfjs', manifest)
    if args.upstream:
        check_upstream(manifest)
    print('Reviewed PDF.js bundle verified' + ('; official metadata is current.' if args.upstream else '.'))


if __name__ == '__main__':
    try:
        main()
    except (ValueError, KeyError, OSError) as error:
        print('PDF.js audit failed: ' + str(error), file=sys.stderr)
        sys.exit(1)
