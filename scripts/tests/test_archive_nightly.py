import copy
from datetime import datetime, timezone, timedelta
import importlib.util
import json
import pathlib
import tempfile
import unittest
from unittest.mock import patch
from zoneinfo import ZoneInfoNotFoundError

spec = importlib.util.spec_from_file_location('archive', pathlib.Path(__file__).resolve().parents[1] / 'archive-nightly.py')
m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m)
SOURCE = 'a' * 40
GROUPING = m.grouping


class FakeGitHub:
    def __init__(self):
        self.items = {}; self.files = {}; self.calls = []; self.corrupt = None

    def add(self, tag='Nightly', stamp='2026-10-15T01:00:00Z', data=b'old package'):
        self.files[tag] = {'old.zip': data}
        self.items[tag] = {'tag_name': tag, 'draft': False, 'body': 'Original build source: `' + SOURCE + '`.\nOriginal notes', 'assets': [{'id': 1, 'name': 'old.zip', 'size': len(data), 'updated_at': stamp}]}

    def gh(self, *args):
        self.calls.append(args)
        if args[0] == 'api': return json.dumps([list(self.items.values())]).encode()
        command, tag = args[1:3]
        if command == 'download':
            dest = pathlib.Path(args[args.index('--dir') + 1])
            names = [args[i+1] for i, a in enumerate(args) if a == '--pattern'] or list(self.files[tag])
            for name in names:
                data = self.files[tag][name]
                if tag.startswith('archive-2026-') and self.corrupt == ('metadata' if name == 'ARCHIVE.json' else 'asset'):
                    data = b'corrupt'
                (dest / name).write_bytes(data)
        elif command == 'create':
            self.items[tag] = {'tag_name': tag, 'draft': True, 'body': '', 'assets': []}; self.files[tag] = {}
        elif command == 'upload':
            for arg in args[3:args.index('--repo')]:
                path = pathlib.Path(arg); name = path.name
                if name in self.files[tag] and '--clobber' not in args: raise ValueError('duplicate upload')
                self.files[tag][name] = path.read_bytes()
            self.items[tag]['assets'] = [{'name': n, 'size': len(b)} for n, b in self.files[tag].items()]
        elif command == 'edit':
            self.items[tag]['draft'] = False
            self.items[tag]['body'] = pathlib.Path(args[args.index('--notes-file')+1]).read_text()
        elif command == 'delete':
            del self.items[tag]; del self.files[tag]
        else: raise AssertionError(args)
        return b''


class Tests(unittest.TestCase):
    def setUp(self):
        self.remote = FakeGitHub(); self.remote.add()
        self.addCleanup(patch.stopall)
        patch.object(m, 'gh', self.remote.gh).start()
        patch.object(m, 'api', return_value={'object': {'type': 'commit', 'sha': SOURCE}}).start()
        patch.dict(m.os.environ, {'SOURCE_SHA': 'publishing-head'}).start()
        # Archive jobs run on Ubuntu with IANA zones; keep unit tests portable on Windows.
        original = m.grouping
        patch.object(m, 'grouping', side_effect=lambda stamp, identity: original(stamp, identity, timezone(timedelta(hours=-5)))).start()
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.incoming = pathlib.Path(self.temp.name); (self.incoming/'new.zip').write_bytes(b'new package')

    def archive(self): m.archive('owner/repo', self.incoming)

    def daily(self): return self.remote.items['archive-2026-10-14']

    def manifest(self): return json.loads(self.remote.files['archive-2026-10-14']['ARCHIVE.json'])

    def test_verified_archive_and_original_provenance(self):
        self.archive()
        create = next(c for c in self.remote.calls if c[:2] == ('release', 'create'))
        self.assertEqual(create[create.index('--target')+1], 'publishing-head')
        self.assertFalse(self.daily()['draft'])
        record = next(iter(self.manifest()['builds'].values()))
        self.assertEqual(record['source'], SOURCE)
        self.assertEqual(record['assets'][0]['originalName'], 'old.zip')
        self.assertIn('Original notes', record['releaseNotes'])
        self.assertEqual(self.remote.files['archive-2026-10-14'][record['assets'][0]['name']], b'old package')

    def test_same_day_appends_without_clobbering_builds(self):
        self.archive()
        first = copy.deepcopy(self.manifest()['builds'])
        self.remote.add(stamp='2026-10-15T02:00:00Z', data=b'second package')
        self.archive()
        self.assertEqual(len(self.manifest()['builds']), 2)
        for key, record in first.items(): self.assertEqual(self.manifest()['builds'][key], record)
        self.assertEqual(sum(c[:2] == ('release', 'create') for c in self.remote.calls), 1)

    def test_retry_reuses_matching_assets(self):
        self.archive(); self.archive()
        self.assertEqual(len(self.manifest()['builds']), 1)
        self.assertEqual(sum(c[:2] == ('release', 'create') for c in self.remote.calls), 1)

    def test_identical_republish_creates_no_archive(self):
        (self.incoming/'new.zip').unlink(); (self.incoming/'old.zip').write_bytes(b'old package')
        self.archive()
        self.assertEqual(set(self.remote.items), {'Nightly'})

    def test_corrupt_assets_do_not_publish_or_delete(self):
        self.remote.corrupt = 'asset'
        with self.assertRaises(ValueError): self.archive()
        self.assertFalse(any(c[:2] in [('release', 'edit'), ('release', 'delete')] for c in self.remote.calls))
        self.assertIn('Nightly', self.remote.items)

    def test_retry_partial_draft_succeeds(self):
        self.remote.corrupt = 'asset'
        with self.assertRaises(ValueError): self.archive()
        self.remote.corrupt = None; self.archive()
        self.assertFalse(self.daily()['draft'])

    def test_corrupt_metadata_does_not_publish(self):
        self.remote.corrupt = 'metadata'
        with self.assertRaises(ValueError): self.archive()
        self.assertFalse(any(c[:2] == ('release', 'edit') for c in self.remote.calls))

    def test_migration_deletes_old_heading_only_after_verification(self):
        self.remote.add('archive-20261015-010000-abcdef123456')
        m.consolidate('owner/repo')
        self.assertNotIn('archive-20261015-010000-abcdef123456', self.remote.items)
        self.assertIn('Nightly', self.remote.items)
        edit = next(i for i,c in enumerate(self.remote.calls) if c[:2] == ('release', 'edit'))
        delete = next(i for i,c in enumerate(self.remote.calls) if c[:2] == ('release', 'delete'))
        self.assertGreater(delete, edit)
        self.assertNotIn('--cleanup-tag', self.remote.calls[delete])

    def test_failed_migration_keeps_old_release(self):
        tag = 'archive-20261015-010000-abcdef123456'; self.remote.add(tag); self.remote.corrupt = 'asset'
        with self.assertRaises(ValueError): m.consolidate('owner/repo')
        self.assertIn(tag, self.remote.items)

    def test_local_midnight_starts_new_group(self):
        self.archive()
        self.remote.add(stamp='2026-10-15T05:01:00Z'); self.archive()
        self.assertIn('archive-2026-10-15', self.remote.items)

    def test_real_chicago_day_and_daylight_saving(self):
        try:
            zone = m.ZoneInfo('America/Chicago')
        except ZoneInfoNotFoundError:
            self.skipTest('Windows unit-test host has no IANA timezone database; archive job runs on Ubuntu')
        before = GROUPING(datetime.fromisoformat('2026-10-15T04:59:00+00:00'), 'abc', zone)
        after = GROUPING(datetime.fromisoformat('2026-10-15T05:01:00+00:00'), 'abc', zone)
        self.assertEqual(before[0], 'archive-2026-10-14')
        self.assertEqual(after[0], 'archive-2026-10-15')
        first = GROUPING(datetime.fromisoformat('2026-11-01T06:30:00+00:00'), 'abc', zone)
        second = GROUPING(datetime.fromisoformat('2026-11-01T07:30:00+00:00'), 'abc', zone)
        self.assertEqual(first[0], second[0]); self.assertNotEqual(first[1], second[1])

    def test_unsafe_asset_names_fail_before_download(self):
        self.remote.items['Nightly']['assets'][0]['name'] = '../package'
        with self.assertRaises(ValueError): self.archive()
        self.assertFalse(any(c[:2] == ('release', 'download') for c in self.remote.calls))

    def test_conflicting_preserved_asset_is_not_overwritten(self):
        self.archive()
        manifest = self.manifest(); key = next(iter(manifest['builds']))
        manifest['builds'][key]['source'] = 'b' * 40
        self.remote.files['archive-2026-10-14']['ARCHIVE.json'] = json.dumps(manifest).encode()
        with self.assertRaises(ValueError): self.archive()


if __name__ == '__main__': unittest.main()
