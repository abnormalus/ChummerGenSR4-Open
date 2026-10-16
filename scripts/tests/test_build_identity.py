#!/usr/bin/env python3
"""Release numbers are deterministic, monotonic and honest about local edits."""
import importlib.util
import json
import pathlib
import subprocess
import tempfile
import unittest
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location('build_identity', pathlib.Path(__file__).resolve().parents[1] / 'build_identity.py')
identity = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(identity)


class BuildIdentityTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = pathlib.Path(self.temp.name)
        self.git('init', '-q')
        self.git('config', 'user.name', 'Build identity tests')
        self.git('config', 'user.email', 'build-tests@example.invalid')
        (self.root / 'release-version.json').write_text('{"major":0,"minor":2}')
        self.commit()

    def tearDown(self):
        self.temp.cleanup()

    def git(self, *args):
        return subprocess.check_output(['git', *args], cwd=self.root, text=True).strip()

    def commit(self):
        self.git('add', '.')
        self.git('commit', '-qm', 'synthetic identity fixture')

    def test_clean_identity(self):
        result = identity.describe(self.root)
        self.assertEqual(result['version'], '0.2.1')
        self.assertEqual(result['fileVersion'], '0.2.1.0')
        self.assertEqual(result['informationalVersion'], '0.2.1-nightly+' + self.git('rev-parse', 'HEAD'))
        self.assertFalse(result['dirty'])

    def test_dirty_is_never_a_release(self):
        (self.root / 'untracked').write_text('synthetic')
        result = identity.describe(self.root, 'Stable')
        self.assertEqual(result['channel'], 'Development')
        self.assertTrue(result['informationalVersion'].endswith('.dirty'))

    def test_source_changes_increment_version(self):
        before = identity.describe(self.root)
        (self.root / 'new').write_text('synthetic'); self.commit()
        after = identity.describe(self.root)
        self.assertGreater(after['buildNumber'], before['buildNumber'])
        self.assertNotEqual(after['sourceCommit'], before['sourceCommit'])

    def test_rebuilding_same_source_is_deterministic(self):
        self.assertEqual(identity.describe(self.root), identity.describe(self.root))

    def test_shallow_history_is_rejected(self):
        with patch.object(identity.subprocess, 'check_output', return_value='true\n'):
            with self.assertRaisesRegex(ValueError, 'full Git history'):
                identity.describe(self.root)

    def test_msi_bounds(self):
        (self.root / 'release-version.json').write_text(json.dumps(dict(major=256, minor=2)))
        with self.assertRaisesRegex(ValueError, 'MSI version limits'):
            identity.describe(self.root)

    def test_stable_requires_explicit_channel(self):
        self.assertEqual(identity.describe(self.root, 'Stable')['channel'], 'Stable')
        self.assertEqual(identity.describe(self.root)['channel'], 'Nightly')


if __name__ == '__main__':
    unittest.main()
