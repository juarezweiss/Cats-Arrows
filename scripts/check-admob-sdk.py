#!/usr/bin/env python3
"""#66: verify exact vendored SDK bytes and prohibit unreviewed dependency setup."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
inventory = json.loads((ROOT / 'docs/evidence/issue66/sdk-inventory.json').read_text())
for item in inventory['files']:
    path = ROOT / item['path']
    if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != item['sha256']:
        raise SystemExit(f'SDK pin mismatch: {item["path"]}')
expected = {item['path'] for item in inventory['files']}
for base in ['UnityProject/ThirdParty~/GoogleMobileAds', 'UnityProject/Assets/ThirdParty/GoogleMobileAds']:
    for path in (ROOT / base).rglob('*'):
        if path.is_file() and path.suffix != '.meta' and path.name not in ['link.xml']:
            relative = path.relative_to(ROOT).as_posix()
            if relative not in expected:
                raise SystemExit(f'Uninspected SDK file: {relative}')
for path in (ROOT / 'UnityProject/Assets/ThirdParty/GoogleMobileAds').glob('*.dll'):
    metadata = path.with_name(path.name + '.meta').read_text()
    if 'isExplicitlyReferenced: 1' not in metadata or 'UNITY_EDITOR || PAWS_ADMOB_REVIEW || PAWS_MONETIZATION' not in metadata:
        raise SystemExit(f'SDK player isolation missing: {path.name}')
manifest = json.loads((ROOT / 'UnityProject/Packages/manifest.json').read_text())
if any('external-dependency-manager' in key or 'ads.mobile' in key for key in manifest['dependencies']):
    raise SystemExit('Unreviewed SDK/dependency-manager installation path.')
print(f'AdMob SDK pins verified: {len(expected)} files; plugin 11.6.0 / GMA 13.11.0 / UMP 3.1.0.')
