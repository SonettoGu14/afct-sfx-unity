#!/usr/bin/env bash
# Regenerates Tests/EditMode/Golden from the web engine.
set -euo pipefail
WEB="${WEB_REPO:-/Users/gyk/GitHubProjects/game-sfx-generator-web}"
cd "$WEB"
npx vitest run tools/generate-golden.test.ts
echo "Fixtures written to $WEB/../afct-sfx-unity/Tests/EditMode/Golden"
