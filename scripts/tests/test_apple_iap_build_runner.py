"""Issue #64 unsigned review build contracts; no Unity/device/store actions."""
import json
import subprocess
import unittest
from pathlib import Path
import test_unity_runners as runners


class AppleIapBuildRunnerTests(unittest.TestCase):
    set_host = runners.UnityRunnerTests.set_host

    def setUp(self):
        runners.UnityRunnerTests.setUp(self)
        self.env['PAWS_APPLE_PRODUCT_ID'] = 'com.jweiss.paws.removeads'
        git = self.bin / 'git'
        git.write_text('''#!/usr/bin/env python3
import os,sys
if 'status' in sys.argv: print(os.environ.get('FIXTURE_DIRTY',''),end='')
elif 'rev-parse' in sys.argv: print('a'*40)
else: sys.exit(9)
''')
        git.chmod(0o755)
        self.editor.write_text('''#!/usr/bin/env python3
import json,os,sys
from pathlib import Path
args=sys.argv[1:];Path(os.environ['FIXTURE_ARGV']).write_text(json.dumps(args))
project=Path(args[args.index('-projectPath')+1])
for relative in ['SceneTemplateSettings.json','Packages/com.unity.services.core/Settings.json']:
 path=project/'ProjectSettings'/relative;path.parent.mkdir(parents=True,exist_ok=True)
 if not path.exists(): path.write_text('' if relative.startswith('Packages') else '{}')
output=Path(os.environ['PAWS_IOS_EXPORT']);output.mkdir()
(output/'Unity-iPhone.xcodeproj').mkdir()
(output/'Unity-iPhone.xcodeproj/project.pbxproj').write_text('fixture')
(output/'paws-iap-review.json').write_text(json.dumps({'sourceSha':os.environ['PAWS_IAP_SOURCE_SHA']}))
sys.exit(int(os.environ.get('FIXTURE_EXIT','0')))
''')
        self.output = self.base / 'IAP review with spaces'

    def run_build(self):
        return subprocess.run(['bash', str(self.repo / 'scripts/build-apple-iap-review.sh'), str(self.output)],
                              cwd=self.other_cwd, env=self.env, capture_output=True, text=True)

    def test_requires_approved_mapping_before_editor(self):
        self.env['PAWS_APPLE_PRODUCT_ID'] = 'invented'
        self.assertEqual(self.run_build().returncode, 2)
        self.assertFalse(Path(self.env['FIXTURE_ARGV']).exists())

    def test_clean_unsigned_export_uses_explicit_review_entry_and_source_sha(self):
        result = self.run_build()
        self.assertEqual(result.returncode, 0, result.stderr)
        args = json.loads(Path(self.env['FIXTURE_ARGV']).read_text())
        self.assertEqual(args[args.index('-executeMethod')+1],
                         'CatsAndArrows.Purchasing.Apple.Review.Editor.AppleIapReviewBuild.Build')
        self.assertEqual(args[args.index('-buildTarget')+1], 'iOS')
        self.assertEqual(json.loads((self.output/'paws-iap-review.json').read_text())['sourceSha'], 'a'*40)
        self.assertFalse((self.repo/'UnityProject/ProjectSettings/SceneTemplateSettings.json').exists())
        self.assertFalse((self.repo/'UnityProject/ProjectSettings/Packages/com.unity.services.core/Settings.json').exists())
        self.assertEqual(self.run_build().returncode, 2)

    def test_dirty_inputs_reject_before_editor(self):
        self.env['FIXTURE_DIRTY'] = ' M UnityProject/source.cs'
        result = self.run_build()
        self.assertEqual(result.returncode, 2, result.stderr)
        self.assertFalse(self.output.exists())

    def test_editor_failure_preserves_exit_and_cleans_generated_defaults(self):
        self.env['FIXTURE_EXIT'] = '7'
        self.assertEqual(self.run_build().returncode, 7)
        self.assertFalse((self.repo/'UnityProject/ProjectSettings/SceneTemplateSettings.json').exists())

    def test_existing_owner_configuration_is_preserved(self):
        settings = self.repo/'UnityProject/ProjectSettings'
        template = settings/'SceneTemplateSettings.json';template.write_text('{"owner":true}')
        core = settings/'Packages/com.unity.services.core/Settings.json';core.parent.mkdir(parents=True);core.write_text('{"owner":true}')
        result = self.run_build()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(template.read_text(), '{"owner":true}')
        self.assertEqual(core.read_text(), '{"owner":true}')
