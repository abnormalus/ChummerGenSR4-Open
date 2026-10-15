import copy
import hashlib
import importlib.util
import pathlib
import tempfile
import unittest

SCRIPT = pathlib.Path(__file__).resolve().parents[1] / 'check-pdfjs.py'
spec = importlib.util.spec_from_file_location('pdfjs_audit', SCRIPT)
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)


class PdfJsSafeguards(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.bundle = pathlib.Path(self.temp.name)
        self.manifest = {'version': '1.2.3', 'tarball': audit.REGISTRY + '-/pdfjs-dist-1.2.3.tgz', 'integrity': 'sha512-' + 'A' * 86 + '=='}
        (self.bundle / 'VERSION.txt').write_text('pdfjs-dist 1.2.3\n' + self.manifest['tarball'] + '\n' + self.manifest['integrity'], encoding='utf-8')
        (self.bundle / 'reader.mjs').write_bytes(b'reviewed code')
        self.manifest['files'] = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in self.bundle.iterdir()}

    def test_reviewed_bundle_passes(self):
        audit.verify(self.bundle, self.manifest)

    def test_changed_missing_and_unreviewed_assets_fail(self):
        for kind in ('changed', 'missing', 'added'):
            with self.subTest(kind=kind):
                path = self.bundle / 'reader.mjs'
                path.write_bytes(b'reviewed code')
                if kind == 'changed': path.write_bytes(b'changed')
                elif kind == 'missing': path.unlink()
                else: (self.bundle / 'extra.mjs').write_bytes(b'unreviewed')
                with self.assertRaises(ValueError): audit.verify(self.bundle, self.manifest)

    def test_unofficial_provenance_fails(self):
        manifest = copy.deepcopy(self.manifest)
        manifest['tarball'] = 'https://example.com/package.tgz'
        with self.assertRaises(ValueError): audit.verify(self.bundle, manifest)

    def metadata(self, url):
        return {'version': '1.2.3', 'dist': {'integrity': self.manifest['integrity'], 'tarball': self.manifest['tarball']}}

    def test_current_official_metadata_passes(self):
        audit.check_upstream(self.manifest, self.metadata)

    def test_new_release_requires_review(self):
        def fetch(url):
            return {'version': '1.2.4'} if url.endswith('latest') else self.metadata(url)
        with self.assertRaisesRegex(ValueError, '1.2.4 is available'):
            audit.check_upstream(self.manifest, fetch)

    def test_changed_package_integrity_fails(self):
        def fetch(url):
            result = self.metadata(url)
            result['dist']['integrity'] = 'changed'
            return result
        with self.assertRaises(ValueError): audit.check_upstream(self.manifest, fetch)

    def test_missing_or_unavailable_metadata_cannot_pass(self):
        with self.assertRaises(KeyError): audit.check_upstream(self.manifest, lambda _: {})
        def unavailable(_): raise OSError('offline')
        with self.assertRaises(OSError): audit.check_upstream(self.manifest, unavailable)


if __name__ == '__main__': unittest.main()
