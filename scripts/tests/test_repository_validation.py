"""Keep Unity-generated UTF-8 BOMs valid without accepting malformed JSON."""

import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


class RepositoryValidationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="repository validation ")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        required = (
            "AGENTS.md", "README.md", "docs/GAME_DESIGN.md",
            "docs/PUZZLE_SOLVABILITY.md", "docs/ARCHITECTURE.md", "docs/TESTING.md",
            "docs/RELEASE.md", ".codex/config.toml",
            ".agents/skills/cats-arrows-development/SKILL.md",
            "UnityProject/Assets/CatsAndArrows/Core/PuzzleDefinition.cs",
            "UnityProject/Assets/CatsAndArrows/Gameplay/GameSession.cs",
        )
        for name in required:
            path = self.root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("# Fixture\n", encoding="utf-8")
        self.manifest = self.root / "UnityProject/Packages/manifest.json"
        self.manifest.parent.mkdir(parents=True)
        self.asmdef = self.root / "UnityProject/Assets/CatsAndArrows/Core/Fixture.asmdef"
        self.scripts = self.root / "scripts"
        self.scripts.mkdir()
        shutil.copy2(Path(__file__).resolve().parents[1] / "validate.sh", self.scripts)

    def validate(self):
        return subprocess.run(["bash", str(self.scripts / "validate.sh")],
                              cwd=self.root.parent, capture_output=True, text=True)

    def test_plain_and_bom_json_are_accepted_without_rewriting(self):
        for manifest_encoding, asmdef_encoding in (("utf-8", "utf-8"), ("utf-8-sig", "utf-8"),
                                                  ("utf-8", "utf-8-sig"), ("utf-8-sig", "utf-8-sig")):
            with self.subTest(manifest=manifest_encoding, asmdef=asmdef_encoding):
                self.manifest.write_text('{"dependencies": {}}', encoding=manifest_encoding)
                self.asmdef.write_text('{"name": "Fixture"}', encoding=asmdef_encoding)
                before = (self.manifest.read_bytes(), self.asmdef.read_bytes())
                result = self.validate()
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual(before, (self.manifest.read_bytes(), self.asmdef.read_bytes()))

    def test_malformed_json_still_fails(self):
        for broken in (self.manifest, self.asmdef):
            with self.subTest(path=broken):
                self.manifest.write_text('{"dependencies": {}}', encoding="utf-8-sig")
                self.asmdef.write_text('{"name": "Fixture"}', encoding="utf-8-sig")
                broken.write_text('{"unfinished":', encoding="utf-8-sig")
                result = self.validate()
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("JSONDecodeError", result.stderr)
