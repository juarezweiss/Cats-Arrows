"""Exercise runner contracts with a process fixture; these are not Unity tests."""

import os
import shutil
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path


SCRIPTS = Path(__file__).resolve().parents[1]
VERSION = "6000.3.25f1"


def report(*results, run_result=None):
    names = {"passed": "Passed", "failed": "Failed", "skipped": "Skipped", "inconclusive": "Inconclusive"}
    attrs = {name: str(results.count(outcome)) for name, outcome in names.items()}
    attrs.update(total=str(len(results)), result=run_result or ("Passed" if all(r == "Passed" for r in results) else "Failed"))
    root = ET.Element("test-run", attrs)
    for index, result in enumerate(results):
        ET.SubElement(root, "test-case", name=f"Fixture{index}", result=result)
    return ET.tostring(root, encoding="unicode")


class UnityRunnerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="unity runner fixture ")
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name)
        self.repo = self.base / "repo with spaces"
        shutil.copytree(SCRIPTS, self.repo / "scripts", ignore=shutil.ignore_patterns("tests", "__pycache__"))
        settings = self.repo / "UnityProject/ProjectSettings"
        settings.mkdir(parents=True)
        (settings / "ProjectVersion.txt").write_text(f"m_EditorVersion: {VERSION}\n")
        self.other_cwd = self.base / "other directory"
        self.other_cwd.mkdir()
        self.bin = self.base / "bin"
        self.bin.mkdir()
        self.hub = self.base / "Hub editors with spaces"
        self.editor = self.hub / VERSION / "Unity.app/Contents/MacOS/Unity"
        self.editor.parent.mkdir(parents=True)
        self.editor.write_text("""#!/usr/bin/env python3
import json, os, sys
from pathlib import Path
args=sys.argv[1:]
Path(os.environ['FIXTURE_ARGV']).write_text(json.dumps(args))
if '-logFile' in args:
 Path(args[args.index('-logFile')+1]).write_text(os.environ.get('FIXTURE_LOG', 'Import completed\\n'))
if '-runTests' in args and 'FIXTURE_XML' in os.environ:
 Path(args[args.index('-testResults')+1]).write_text(os.environ['FIXTURE_XML'])
sys.exit(int(os.environ.get('FIXTURE_EXIT', '0')))
""")
        self.editor.chmod(0o755)
        self.env = dict(os.environ)
        for name in ("UNITY_EDITOR", "UNITY_HUB_EDITOR_ROOT", "FIXTURE_XML", "FIXTURE_EXIT", "FIXTURE_LOG"):
            self.env.pop(name, None)
        self.env.update(UNITY_HUB_EDITOR_ROOT=str(self.hub), FIXTURE_ARGV=str(self.base / "args.json"))
        self.env["PATH"] = str(self.bin) + os.pathsep + self.env["PATH"]
        self.set_host("Darwin")

    def set_host(self, host):
        script = self.bin / "uname"
        script.write_text(f"#!/bin/sh\nprintf '%s\\n' '{host}'\n")
        script.chmod(0o755)

    def run_script(self, name):
        return subprocess.run(["bash", str(self.repo / "scripts" / name)], cwd=self.other_cwd,
                              env=self.env, text=True, capture_output=True)

    def arguments(self):
        import json
        return json.loads((self.base / "args.json").read_text())

    def test_pinned_mac_editor_and_explicit_override(self):
        found = self.run_script("unity.sh")
        self.assertEqual(found.returncode, 0, found.stderr)
        self.assertEqual(found.stdout.strip(), str(self.editor))
        self.env["UNITY_EDITOR"] = str(self.editor)
        self.env["UNITY_HUB_EDITOR_ROOT"] = "/missing"
        overridden = self.run_script("unity.sh")
        self.assertEqual(overridden.returncode, 0, overridden.stderr)
        self.assertEqual(overridden.stdout.strip(), str(self.editor))

    def test_pinned_linux_editor(self):
        self.set_host("Linux")
        editor = self.hub / VERSION / "Editor/Unity"
        editor.parent.mkdir(parents=True)
        shutil.copy2(self.editor, editor)
        found = self.run_script("unity.sh")
        self.assertEqual(found.returncode, 0, found.stderr)
        self.assertEqual(found.stdout.strip(), str(editor))

    def test_invalid_override_never_falls_back(self):
        nonexecutable = self.base / "nonexecutable Unity"
        nonexecutable.write_text("not an executable")
        nonexecutable.chmod(0o644)
        for path in ("relative/Unity", "/missing/Unity", str(nonexecutable)):
            with self.subTest(path=path):
                self.env["UNITY_EDITOR"] = path
                result = self.run_script("unity.sh")
                self.assertEqual(result.returncode, 2)
                self.assertIn("absolute path to an executable", result.stderr)

    def test_missing_pinned_editor_does_not_choose_another_patch(self):
        self.env["UNITY_HUB_EDITOR_ROOT"] = str(self.base / "missing editors")
        result = self.run_script("unity.sh")
        self.assertEqual(result.returncode, 2)
        self.assertIn(VERSION, result.stderr)

    def test_editmode_cwd_spaces_and_fresh_results(self):
        self.env["FIXTURE_XML"] = report("Passed", "Passed")
        first = self.run_script("test.sh")
        self.assertEqual(first.returncode, 0, first.stderr)
        args = self.arguments()
        self.assertNotIn("-quit", args)
        self.assertEqual(args[args.index("-projectPath") + 1], str(self.repo / "UnityProject"))
        self.assertEqual(args[args.index("-testPlatform") + 1], "EditMode")
        old_report = Path(args[args.index("-testResults") + 1])
        self.assertTrue(old_report.exists())
        # A prior passing report cannot rescue a later process that emits no report.
        self.env.pop("FIXTURE_XML")
        second = self.run_script("test.sh")
        self.assertNotEqual(second.returncode, 0)
        args = self.arguments()
        self.assertNotEqual(str(old_report), args[args.index("-testResults") + 1])

    def test_bad_reports_fail_even_when_editor_returns_zero(self):
        for xml in ("not XML", report(), report("Skipped"), report("Passed", "Skipped"),
                    report("Inconclusive"), report("Passed", "Failed"),
                    report("Passed", run_result="Failed"),
                    report("Passed").replace('total="1"', 'total="99"')):
            with self.subTest(xml=xml):
                self.env["FIXTURE_XML"] = xml
                result = self.run_script("test.sh")
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("Unity results validation failed", result.stderr)

    def test_unity_error_status_is_preserved(self):
        self.env["FIXTURE_EXIT"] = "13"
        for xml in (None, report("Passed"), "broken"):
            with self.subTest(xml=xml):
                if xml is None:
                    self.env.pop("FIXTURE_XML", None)
                else:
                    self.env["FIXTURE_XML"] = xml
                result = self.run_script("test.sh")
                self.assertEqual(result.returncode, 13, result.stderr)

    def test_compile_has_quit_and_retains_log(self):
        result = self.run_script("compile.sh")
        self.assertEqual(result.returncode, 0, result.stderr)
        args = self.arguments()
        self.assertIn("-quit", args)
        self.assertNotIn("-runTests", args)
        self.assertEqual(args[args.index("-projectPath") + 1], str(self.repo / "UnityProject"))
        self.assertTrue(Path(args[args.index("-logFile") + 1]).read_text())

    def test_compile_process_errors_and_missing_or_error_logs(self):
        self.env["FIXTURE_EXIT"] = "9"
        self.assertEqual(self.run_script("compile.sh").returncode, 9)
        self.env["FIXTURE_EXIT"] = "0"
        for log in ("", "Assets/Code.cs(1,2): error CS1002: ; expected\n", "Scripts have compiler errors\n"):
            with self.subTest(log=log):
                self.env["FIXTURE_LOG"] = log
                self.assertNotEqual(self.run_script("compile.sh").returncode, 0)


if __name__ == "__main__":
    unittest.main()
