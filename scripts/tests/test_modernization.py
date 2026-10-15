import importlib.util
import pathlib
import unittest
from unittest.mock import patch

ROOT = pathlib.Path(__file__).resolve().parents[1]
def load(name):
    spec = importlib.util.spec_from_file_location(name, ROOT / (name + ".py"))
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module); return module
source = load("check-modernization")
audit = load("audit-dependencies")
identity = load("write-build-revision")

class Safeguards(unittest.TestCase):
    def test_obsolete_and_unsupported_calls_fail(self):
        for code in ("new WebClient();", "new RNGCryptoServiceProvider();", "new BinaryFormatter();", "new PrintDialog();", "refresh.BeginInvoke(null, null);", "Application.DoEvents();"):
            with self.subTest(code=code): self.assertTrue(source.source_errors("Chummer.Mac/code/test.cs", code))
    def test_dispatcher_and_documented_comments_stay_valid(self):
        self.assertFalse(source.source_errors("Chummer.Mac/code/test.cs", 'owner.BeginInvoke(new Action(Update)); // new WebClient();\nvar text = "new BinaryFormatter()";'))
    def test_suppression_requires_explicit_legacy_exception(self):
        self.assertTrue(source.source_errors("Chummer.Mac/code/test.cs", "#pragma warning disable SYSLIB0014"))
        self.assertTrue(source.source_errors("Chummer.Mac/code/test.cs", "#pragma warning disable"))
        self.assertFalse(source.source_errors(source.EXPERIMENTAL_READER, "#pragma warning disable SYSLIB5005"))
    def test_existing_debt_cannot_grow(self):
        for path, exception in source.EXCEPTIONS["manual message pumping"].items():
            with self.subTest(path=path):
                self.assertFalse(source.source_errors(path, "Application.DoEvents();" * exception["count"]))
                self.assertTrue(source.source_errors(path, "Application.DoEvents();" * (exception["count"] + 1)))
        self.assertTrue(source.source_errors("Chummer.Mac/code/frmPrintMultiple.cs", "Application.DoEvents();"))
    def test_build_identity_only_claims_clean_revision(self):
        with patch.object(identity.subprocess, "check_output", side_effect=["a" * 40 + "\n", ""]):
            self.assertEqual(identity.revision(), "a" * 40)
        with patch.object(identity.subprocess, "check_output", side_effect=["a" * 40 + "\n", "?? new-code.cs\n"]):
            self.assertEqual(identity.revision(), "development")
    def test_clean_dependency_report(self):
        audit.validate({"version":1,"sources":["nuget"],"projects":[{"path":"/repo/app.csproj"}]}, "app.csproj")
    def test_transitive_findings_fail(self):
        report={"version":1,"sources":["nuget"],"projects":[{"path":"app.csproj","frameworks":[{"transitivePackages":[{"id":"old","resolvedVersion":"1.0"}]}]}]}
        with self.assertRaises(ValueError): audit.validate(report,"app.csproj")
    def test_unavailable_metadata_cannot_pass(self):
        for report in ({}, {"version":1,"sources":["nuget"],"projects":[]}, {"version":1,"sources":["nuget"],"projects":[{"path":"app.csproj"}],"logs":[{"level":"Warning"}]}):
            with self.subTest(report=report), self.assertRaises(ValueError): audit.validate(report,"app.csproj")

if __name__ == "__main__": unittest.main()
