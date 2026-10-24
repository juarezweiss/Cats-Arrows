"""Issue #63 shell integration contracts; no actual build artifacts or credentials."""
import json
import subprocess
import unittest
from pathlib import Path
import test_unity_runners as runners


class IOSBuildRunnerTests(unittest.TestCase):
    set_host = runners.UnityRunnerTests.set_host

    def setUp(self):
        runners.UnityRunnerTests.setUp(self)
        self.env['UNITY_EDITOR'] = str(self.editor)
        self.env['PAWS_MONETIZATION_PROFILE'] = 'disabled'
        self.env.pop('PAWS_MONETIZATION_CONFIG', None)
        for key in ('PAWS_IOS_TEAM_ID', 'PAWS_IOS_SIGNING_IDENTITY', 'PAWS_IOS_PROFILE'):
            self.env.pop(key, None)
        self.editor.write_text('''#!/usr/bin/env python3
import os,sys,json
from pathlib import Path
args=sys.argv[1:]
Path(os.environ['FIXTURE_ARGV']).write_text(json.dumps(args))
Path(os.environ['FIXTURE_ARGV']+'.profile').write_text(os.environ['PAWS_MONETIZATION_PROFILE'])
project=Path(args[args.index('-projectPath')+1])
for relative in ['SceneTemplateSettings.json','Packages/com.unity.services.core/Settings.json']:
 path=project/'ProjectSettings'/relative;path.parent.mkdir(parents=True,exist_ok=True)
 if not path.exists(): path.write_text(os.environ.get('FIXTURE_CORE_CONTENT','') if relative.startswith('Packages') else '{}')
Path(os.environ['PAWS_IOS_EXPORT']).mkdir()
sys.exit(int(os.environ.get('FIXTURE_EXIT','0')))
''')
        inspector = self.repo / 'scripts/ios-release.py'
        inspector.write_text('''import sys
from pathlib import Path
args=sys.argv
output=Path(args[args.index('--output')+1])
for flag in ('--export','--archive'):
 if flag in args and output.is_relative_to(Path(args[args.index(flag)+1])):
  sys.exit(9)
output.write_text('{}')
''')
        xcode = self.bin / 'xcodebuild'
        xcode.write_text('''#!/usr/bin/env python3
import os,sys,json
from pathlib import Path
args=sys.argv[1:]
Path(os.environ['FIXTURE_ARGV']).write_text(json.dumps(args))
Path(args[args.index('-archivePath')+1]).mkdir()
''')
        xcode.chmod(0o755)
        self.export = self.base / 'export with spaces'
        self.archive = self.base / 'Paws build.xcarchive'

    def run_build(self, *args):
        return subprocess.run(['bash', str(self.repo / 'scripts/build-ios.sh'), *map(str, args)],
                              cwd=self.other_cwd, env=self.env, capture_output=True, text=True)

    def test_export_uses_pinned_entry_and_external_receipt(self):
        result = self.run_build('export', self.export)
        self.assertEqual(result.returncode, 0, result.stderr)
        args = json.loads(Path(self.env['FIXTURE_ARGV']).read_text())
        self.assertEqual(args[args.index('-executeMethod')+1], 'CatsAndArrows.UI.Editor.ProductionIOSBuild.Build')
        self.assertEqual(args[args.index('-buildTarget')+1], 'iOS')
        self.assertEqual(len(list((self.repo / '.unity-results').rglob('export-receipt.json'))), 1)
        self.assertEqual(list(self.export.iterdir()), [])
        again = self.run_build('export', self.export)
        self.assertEqual(again.returncode, 2)

    def test_failed_unity_does_not_generate_success_receipt(self):
        self.env['FIXTURE_EXIT'] = '7'
        result = self.run_build('export', self.export)
        self.assertEqual(result.returncode, 7)
        self.assertEqual(list((self.repo / '.unity-results').rglob('*receipt.json')), [])

    def test_production_default_fails_before_export_without_local_configuration(self):
        self.env.pop('PAWS_MONETIZATION_PROFILE')
        result = self.run_build('export', self.export)
        self.assertEqual(result.returncode, 2)
        self.assertIn('PAWS_MONETIZATION_CONFIG', result.stderr)
        self.assertFalse(self.export.exists())

    def test_profile_is_forwarded_without_reading_configuration_into_console(self):
        for profile in ('validation', 'production'):
            with self.subTest(profile=profile):
                self.env['PAWS_MONETIZATION_PROFILE'] = profile
                if profile == 'production':
                    config = self.base / 'private-config.json'
                    config.write_text('{"fixture":"DO-NOT-LOG"}')
                    self.env['PAWS_MONETIZATION_CONFIG'] = str(config)
                output = self.base / ('export-' + profile)
                result = self.run_build('export', output)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual(Path(self.env['FIXTURE_ARGV']+'.profile').read_text(), profile)
                self.assertNotIn('DO-NOT-LOG', result.stdout + result.stderr)

    def test_unknown_profile_is_rejected(self):
        self.env['PAWS_MONETIZATION_PROFILE'] = 'simulated'
        result = self.run_build('export', self.export)
        self.assertEqual(result.returncode, 2)
        self.assertFalse(self.export.exists())

    def test_new_editor_defaults_are_removed_after_success_and_failure(self):
        for exit_code in (0, 7):
            with self.subTest(exit_code=exit_code):
                self.env['FIXTURE_EXIT'] = str(exit_code)
                result = self.run_build('export', self.base / ('export-' + str(exit_code)))
                self.assertEqual(result.returncode, exit_code, result.stderr)
                settings = self.repo / 'UnityProject/ProjectSettings'
                self.assertFalse((settings / 'SceneTemplateSettings.json').exists())
                self.assertFalse((settings / 'Packages/com.unity.services.core/Settings.json').exists())

    def test_existing_owner_defaults_and_new_nonempty_core_configuration_are_preserved(self):
        settings = self.repo / 'UnityProject/ProjectSettings'
        template = settings / 'SceneTemplateSettings.json'
        template.write_text('{"owner":true}')
        core = settings / 'Packages/com.unity.services.core/Settings.json'
        core.parent.mkdir(parents=True)
        core.write_text('{"owner":true}')
        result = self.run_build('export', self.export)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(template.read_text(), '{"owner":true}')
        self.assertEqual(core.read_text(), '{"owner":true}')
        core.unlink()
        self.env['FIXTURE_CORE_CONTENT'] = '{"generated-nonempty":true}'
        result = self.run_build('export', self.base / 'new-core-export')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(core.read_text(), '{"generated-nonempty":true}')

    def test_unsigned_archive_is_release_without_credentials_or_identifier_overrides(self):
        self.export.mkdir()
        result = self.run_build('archive-unsigned', self.export, self.archive)
        self.assertEqual(result.returncode, 0, result.stderr)
        args = json.loads(Path(self.env['FIXTURE_ARGV']).read_text())
        self.assertIn('CODE_SIGNING_ALLOWED=NO', args)
        self.assertIn('Release', args)
        self.assertNotIn('-allowProvisioningUpdates', args)
        self.assertFalse(any('PRODUCT_BUNDLE_IDENTIFIER' in arg for arg in args))
        self.assertEqual(len(list((self.repo / '.unity-results').rglob('release-receipt.json'))), 1)
        self.assertEqual(list(self.archive.iterdir()), [])

    def test_signed_archive_requires_local_inputs_and_scopes_profile_to_app(self):
        self.export.mkdir()
        missing = self.run_build('archive-signed', self.export, self.archive)
        self.assertEqual(missing.returncode, 2)
        self.assertIn('Missing local signing input', missing.stderr)
        self.assertFalse(self.archive.exists())
        self.env.update(PAWS_IOS_TEAM_ID='FIXTURE', PAWS_IOS_SIGNING_IDENTITY='Fixture Identity', PAWS_IOS_PROFILE='Fixture Profile')
        result = self.run_build('archive-signed', self.export, self.archive)
        self.assertEqual(result.returncode, 0, result.stderr)
        args = json.loads(Path(self.env['FIXTURE_ARGV']).read_text())
        self.assertIn('PROVISIONING_PROFILE_SPECIFIER_APP=Fixture Profile', args)
        self.assertFalse(any(arg.startswith('PROVISIONING_PROFILE_SPECIFIER=') for arg in args))
