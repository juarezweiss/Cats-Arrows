#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

required=(
  AGENTS.md
  README.md
  docs/GAME_DESIGN.md
  docs/PUZZLE_SOLVABILITY.md
  docs/ARCHITECTURE.md
  docs/TESTING.md
  docs/RELEASE.md
  .codex/config.toml
  .agents/skills/cats-arrows-development/SKILL.md
  UnityProject/Assets/CatsAndArrows/Core/PuzzleDefinition.cs
  UnityProject/Assets/CatsAndArrows/Gameplay/GameSession.cs
)

for path in "${required[@]}"; do
  [[ -f "$path" ]] || { echo "Missing required file: $path" >&2; exit 1; }
done

if grep -RIn --exclude-dir=.git --exclude=validate.sh -E 'TODO:|\[TODO' AGENTS.md docs .codex .agents UnityProject/Assets 2>/dev/null; then
  echo "Unresolved TODO marker found in controlled project files." >&2
  exit 1
fi

python3 - <<'PY'
import json, tomllib
from pathlib import Path
for path in Path('UnityProject').rglob('*.asmdef'):
    json.loads(path.read_text(encoding='utf-8-sig'))
json.loads(Path('UnityProject/Packages/manifest.json').read_text(encoding='utf-8-sig'))
tomllib.loads(Path('.codex/config.toml').read_text())
for path in Path('.codex/agents').glob('*.toml'):
    data = tomllib.loads(path.read_text())
    for key in ('name', 'description', 'developer_instructions'):
        if not data.get(key):
            raise SystemExit(f'{path}: missing {key}')
print('JSON/TOML validation: OK')
PY

echo "Repository structure validation: OK"
