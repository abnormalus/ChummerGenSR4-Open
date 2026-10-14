#!/usr/bin/env python3
"""Package a self-contained win-x64 publish directory using msitools/wixl."""
import argparse
import hashlib
import pathlib
import subprocess
import uuid
import xml.etree.ElementTree as ET

NS = 'http://schemas.microsoft.com/wix/2006/wi'
ET.register_namespace('', NS)
UPGRADE = uuid.UUID('df831755-95a2-4ad8-971a-106c01d8b749')


def add(parent, tag, **attributes):
    return ET.SubElement(parent, '{%s}%s' % (NS, tag), attributes)


def ident(prefix, value):
    return prefix + hashlib.sha256(value.encode()).hexdigest()[:28]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('publish', type=pathlib.Path)
    parser.add_argument('output', type=pathlib.Path)
    parser.add_argument('--version', default='0.1.501')
    parser.add_argument('--compiler', default='wixl')
    args = parser.parse_args()
    source = args.publish.resolve()
    output = args.output.resolve()
    if not (source / 'ChummerMac.exe').is_file():
        parser.error('Publish win-x64 first; ChummerMac.exe is missing.')
    if output.exists():
        parser.error('Choose a new installer output path.')
    output.parent.mkdir(parents=True, exist_ok=True)
    root = ET.Element('{%s}Wix' % NS)
    product = add(root, 'Product', Id='*', Name='Chummer SR4', Language='1033',
                  Version=args.version, Manufacturer='Chummer SR4 Community', UpgradeCode=str(UPGRADE).upper())
    add(product, 'Package', Id='*', InstallerVersion='500', Compressed='yes', InstallScope='perUser',
        Description='Chummer SR4 character manager')
    add(product, 'MajorUpgrade', DowngradeErrorMessage='A newer version of Chummer SR4 is already installed.', AllowSameVersionUpgrades='yes')
    add(product, 'MediaTemplate', EmbedCab='yes')
    add(product, 'Property', Id='ARPNOMODIFY', Value='1')
    add(product, 'Property', Id='ARPINSTALLLOCATION', Value='[INSTALLDIR]')
    add(product, 'Icon', Id='ChummerIcon', SourceFile=str(source / 'branding/chummer.ico'))
    add(product, 'Property', Id='ARPPRODUCTICON', Value='ChummerIcon')
    condition = add(product, 'Condition', Message='Chummer SR4 requires 64-bit Windows.')
    condition.text = 'Installed OR VersionNT64'
    target = add(product, 'Directory', Id='TARGETDIR', Name='SourceDir')
    local = add(target, 'Directory', Id='LocalAppDataFolder')
    programs = add(local, 'Directory', Id='UserPrograms', Name='Programs')
    install = add(programs, 'Directory', Id='INSTALLDIR', Name='ChummerSR4')
    menu = add(add(target, 'Directory', Id='ProgramMenuFolder'), 'Directory', Id='ChummerMenu', Name='Chummer SR4')
    directories = {'': install}
    feature = add(product, 'Feature', Id='Complete', Title='Chummer SR4', Level='1')
    count = 0
    for file in sorted(source.rglob('*')):
        if not file.is_file():
            continue
        relative = file.relative_to(source)
        parent = install
        path = pathlib.PurePosixPath()
        for name in relative.parts[:-1]:
            path = path / name
            key = str(path)
            if key not in directories:
                directories[key] = add(parent, 'Directory', Id=ident('D', key), Name=name)
            parent = directories[key]
        component_id = ident('C', relative.as_posix())
        component = add(parent, 'Component', Id=component_id,
                        Guid=str(uuid.uuid5(UPGRADE, relative.as_posix())).upper(), Win64='yes')
        add(component, 'File', Id=ident('F', relative.as_posix()), Name=file.name, Source=str(file))
        add(component, 'RegistryValue', Root='HKCU', Key='Software\\ChummerSR4\\Installer\\Files',
            Name=component_id, Type='integer', Value='1', KeyPath='yes')
        add(feature, 'ComponentRef', Id=component_id)
        count += 1
    shortcuts = add(menu, 'Component', Id='ChummerShortcut', Guid=str(uuid.uuid5(UPGRADE, 'start-menu-shortcut')).upper(), Win64='yes')
    add(shortcuts, 'Shortcut', Id='LaunchChummer', Name='Chummer SR4', Description='Shadowrun fourth edition character manager',
        Target='[INSTALLDIR]ChummerMac.exe', WorkingDirectory='INSTALLDIR', Icon='ChummerIcon', IconIndex='0')
    add(shortcuts, 'RemoveFolder', Id='RemoveChummerMenu', On='uninstall')
    add(shortcuts, 'RegistryValue', Root='HKCU', Key='Software\\ChummerSR4\\Installer', Name='Installed', Type='integer', Value='1', KeyPath='yes')
    add(feature, 'ComponentRef', Id='ChummerShortcut')
    ET.indent(root)
    wxs = output.with_suffix('.wxs')
    ET.ElementTree(root).write(wxs, encoding='utf-8', xml_declaration=True)
    subprocess.run([args.compiler, '-a', 'x64', '-o', str(output), str(wxs)], check=True)
    print('Built %s with %d files' % (output, count))


if __name__ == '__main__':
    main()
