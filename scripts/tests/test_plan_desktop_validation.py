import datetime as dt
import importlib.util
import pathlib
import unittest

path = pathlib.Path(__file__).resolve().parents[1] / "plan-desktop-validation.py"
spec = importlib.util.spec_from_file_location("planner", path)
planner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(planner)

NOW = dt.datetime(2026, 11, 18, tzinfo=dt.timezone.utc)


def example(count=5):
    runs = [{"event": "push", "head_branch": "master", "head_sha": f"{i:040x}", "id": i}
            for i in range(count)]
    jobs = {run["id"]: [{"name": f"validate ({platform})", "conclusion": "success",
                         "completed_at": (NOW - dt.timedelta(days=1)).isoformat()}
                        for platform in planner.PLATFORMS] for run in runs}
    return runs, jobs


class DesktopValidationPlanTests(unittest.TestCase):
    def decide(self, runs, jobs, changed=lambda _: False):
        return planner.plan(runs, jobs.__getitem__, changed, NOW)

    def test_reuse_five_recent_real_passes(self):
        self.assertEqual(self.decide(*example()), dict.fromkeys(planner.PLATFORMS))

    def test_changed_inputs_run_both_platforms(self):
        decisions = self.decide(*example(), changed=lambda _: True)
        self.assertTrue(all(decisions.values()))

    def test_failed_job_only_invalidates_its_own_lane(self):
        runs, jobs = example(6)
        jobs[1][0]["conclusion"] = "failure"
        decisions = self.decide(runs, jobs)
        self.assertIsNotNone(decisions["macos-latest"])
        self.assertIsNone(decisions["windows-latest"])

    def test_cancelled_job_requires_fresh_validation(self):
        runs, jobs = example()
        jobs[0][0]["conclusion"] = "cancelled"
        self.assertIsNotNone(self.decide(runs, jobs)["macos-latest"])

    def test_skipped_jobs_dont_count_toward_streak(self):
        runs, jobs = example()
        jobs[0][0]["conclusion"] = "skipped"
        decisions = self.decide(runs, jobs)
        self.assertIsNotNone(decisions["macos-latest"])
        self.assertIsNone(decisions["windows-latest"])

    def test_untrusted_refs_dont_count(self):
        runs, jobs = example()
        runs[0]["event"] = "pull_request"
        runs[1]["head_branch"] = "topic"
        self.assertTrue(all(self.decide(runs, jobs).values()))

    def test_old_result_runs_full_lane(self):
        runs, jobs = example()
        jobs[0][0]["completed_at"] = (NOW - planner.MAX_AGE).isoformat()
        decisions = self.decide(runs, jobs)
        self.assertIsNotNone(decisions["macos-latest"])
        self.assertIsNone(decisions["windows-latest"])

    def test_docs_and_companions_are_independent(self):
        self.assertFalse(planner.desktop_inputs_changed(
            ["Chummer.PWA/dist/index.html", "Chummer.iOS/Package.swift", "docs/RELEASING.md", "README.md"]))

    def test_unknown_and_shared_paths_always_invalidate(self):
        for name in ["Chummer.Mac/code/frmMain.cs", "vendor/package", "scripts/foo.py",
                     ".github/workflows/desktop-build.yml", ".gitattributes", "new-file"]:
            with self.subTest(path=name):
                self.assertTrue(planner.desktop_inputs_changed([name]))


if __name__ == "__main__":
    unittest.main()
