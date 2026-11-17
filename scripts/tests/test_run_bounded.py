import pathlib
import subprocess
import sys
import tempfile
import unittest

RUNNER = pathlib.Path(__file__).resolve().parents[1] / "run-bounded.py"


class BoundedChecks(unittest.TestCase):
    def run_check(self, code, *options):
        return subprocess.run([sys.executable, str(RUNNER), "--label", "synthetic",
                               "--timeout", "3", "--idle-timeout", "2", *options,
                               "--", sys.executable, "-u", "-c", code],
                              capture_output=True, text=True, errors="replace", timeout=12)

    def test_output_and_exit_code_are_preserved(self):
        result = self.run_check("import sys; print('stdout'); print('stderr',file=sys.stderr); sys.exit(7)")
        self.assertEqual(result.returncode, 7)
        self.assertIn("stdout", result.stdout)
        self.assertIn("stderr", result.stdout)
        self.assertIn("EXIT 7", result.stdout)

    def test_quiet_hang_has_progress_and_idle_deadline(self):
        result = self.run_check("import time; time.sleep(30)",
                                "--idle-timeout", "0.3", "--heartbeat", "0.1")
        self.assertEqual(result.returncode, 124)
        self.assertIn("RUNNING", result.stdout)
        self.assertIn("TIMEOUT (no output)", result.stdout)

    def test_noisy_hang_cannot_extend_overall_deadline(self):
        result = self.run_check("import time\nwhile True: print('active',flush=True); time.sleep(0.02)",
                                "--timeout", "0.4")
        self.assertEqual(result.returncode, 124)
        self.assertIn("TIMEOUT (overall deadline)", result.stdout)

    def test_partial_line_is_forwarded_before_idle_deadline(self):
        result = self.run_check("import sys,time\nfor i in range(6): sys.stdout.write('.'); sys.stdout.flush(); time.sleep(0.1)",
                                "--idle-timeout", "0.4")
        self.assertEqual(result.returncode, 0, result.stdout)
        self.assertIn("......", result.stdout)

    def test_invalid_deadline_and_missing_command_fail(self):
        for arguments in (["--timeout", "nan"], ["--timeout", "0"], ["--timeout", "3"]):
            result = subprocess.run([sys.executable, str(RUNNER), "--label", "synthetic", *arguments],
                                    capture_output=True, text=True, timeout=5)
            self.assertEqual(result.returncode, 2)

    def test_log_retains_raw_output_status_and_overwrites_previous_run(self):
        with tempfile.TemporaryDirectory() as directory:
            log = pathlib.Path(directory) / "nested" / "stage.log"
            first = self.run_check("import os,sys; os.write(1,b'raw\\xff'); sys.exit(7)",
                                   "--log-file", str(log))
            # Read bytes: logs preserve child output even when it is not UTF-8.
            self.assertEqual(first.returncode, 7)
            self.assertIn(b"raw\xff", log.read_bytes())
            self.assertIn(b"EXIT 7", log.read_bytes())
            second = self.run_check("print('new run')", "--log-file", str(log))
            self.assertEqual(second.returncode, 0)
            self.assertIn(b"new run", log.read_bytes())
            self.assertNotIn(b"raw\xff", log.read_bytes())

    def test_timeout_log_retains_last_check_and_timeout_reason(self):
        with tempfile.TemporaryDirectory() as directory:
            log = pathlib.Path(directory) / "stage.log"
            result = self.run_check("import time; print('last completed check',flush=True); time.sleep(30)",
                                    "--idle-timeout", "0.3", "--heartbeat", "0.1",
                                    "--log-file", str(log))
            self.assertEqual(result.returncode, 124)
            self.assertIn("last completed check", log.read_text())
            self.assertIn("RUNNING", log.read_text())
            self.assertIn("TIMEOUT (no output)", log.read_text())


if __name__ == "__main__":
    unittest.main()
