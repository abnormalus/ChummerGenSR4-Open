#!/usr/bin/env python3
"""Verify MSI architecture, embedded Windows manifest, and all payload hashes."""
import argparse
import hashlib
import pathlib
import struct
import subprocess
import tempfile


def manifest_resources(executable):
    data = executable.read_bytes()
    pe = struct.unpack_from('<I', data, 60)[0]
    count = struct.unpack_from('<H', data, pe + 6)[0]
    optional = pe + 24
    size = struct.unpack_from('<H', data, pe + 20)[0]
    directories = optional + (112 if struct.unpack_from('<H', data, optional)[0] == 0x20b else 96)
    resource_rva = struct.unpack_from('<I', data, directories + 16)[0]
    sections = optional + size

    def offset(rva):
        for index in range(count):
            virtual_size, address, raw_size, raw = struct.unpack_from('<IIII', data, sections + 40 * index + 8)
            if address <= rva < address + max(virtual_size, raw_size):
                return raw + rva - address
        raise ValueError('Resource RVA is outside executable sections')

    root = offset(resource_rva)

    def walk(directory, kind=None):
        entries = sum(struct.unpack_from('<HH', data, root + directory + 12))
        for index in range(entries):
            name, target = struct.unpack_from('<II', data, root + directory + 16 + 8 * index)
            resource_type = name if kind is None else kind
            if target & 0x80000000:
                yield from walk(target & 0x7fffffff, resource_type)
            elif resource_type == 24:
                rva, length = struct.unpack_from('<II', data, root + target)
                yield data[offset(rva):offset(rva) + length]

    return b'\n'.join(walk(0))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('publish', type=pathlib.Path)
    parser.add_argument('msi', type=pathlib.Path)
    args = parser.parse_args()
    manifest = manifest_resources(args.publish / 'ChummerMac.exe')
    assert b'8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a' in manifest.lower(), 'Windows compatibility manifest missing'
    assert b'PerMonitorV2' in manifest and b'asInvoker' in manifest, 'DPI or standard-user manifest declaration missing'
    summary = subprocess.check_output(['msiinfo', 'suminfo', str(args.msi)], text=True)
    assert 'Template: x64;1033' in summary, 'MSI must target x64'
    source = {file.relative_to(args.publish): file for file in args.publish.rglob('*') if file.is_file()}
    with tempfile.TemporaryDirectory(prefix='chummer-msi-') as temporary:
        subprocess.run(['msiextract', '-C', temporary, str(args.msi)], check=True, stdout=subprocess.DEVNULL)
        root = pathlib.Path(temporary) / 'Programs/ChummerSR4'
        embedded = {file.relative_to(root): file for file in root.rglob('*') if file.is_file()}
        assert source.keys() == embedded.keys(), 'MSI file list differs from publish output'
        for path in source:
            assert hashlib.sha256(source[path].read_bytes()).digest() == hashlib.sha256(embedded[path].read_bytes()).digest(), str(path)
    print('Verified x64 MSI, Windows manifest, and all %d payload files' % len(source))


if __name__ == '__main__':
    main()
