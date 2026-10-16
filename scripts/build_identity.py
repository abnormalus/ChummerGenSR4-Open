#!/usr/bin/env python3
"""One deterministic source identity for diagnostics, binaries and desktop packages."""
import argparse
import json
import pathlib
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[1]


def describe(root=ROOT, channel='Nightly'):
    def git(*args):
        return subprocess.check_output(['git', *args], cwd=root, text=True).strip()
    if git('rev-parse', '--is-shallow-repository') == 'true':
        raise ValueError('Release identity requires full Git history (checkout fetch-depth: 0).')
    epoch = json.loads((root / 'release-version.json').read_text())
    height = int(git('rev-list', '--first-parent', '--count', 'HEAD'))
    major, minor = int(epoch['major']), int(epoch['minor'])
    if not (0 <= major <= 255 and 0 <= minor <= 255 and 0 < height <= 65535):
        raise ValueError('Release version exceeds Windows MSI version limits.')
    commit = git('rev-parse', 'HEAD')
    dirty = bool(git('status', '--porcelain'))
    channel = 'Development' if dirty else channel
    version = f'{major}.{minor}.{height}'
    return dict(version=version, fileVersion=version + '.0', buildNumber=height,
                informationalVersion=f'{version}-{channel.lower()}+{commit}' + ('.dirty' if dirty else ''),
                sourceCommit=commit, channel=channel, dirty=dirty)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--channel', choices=['Nightly', 'Stable', 'Development'], default='Nightly')
    parser.add_argument('--output', type=pathlib.Path)
    args = parser.parse_args()
    identity = describe(channel=args.channel)
    result = json.dumps(identity, indent=2) + '\n'
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(result, encoding='utf-8')
    else:
        print(result, end='')
