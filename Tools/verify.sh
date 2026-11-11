#!/usr/bin/env bash
# Compiles Runtime/ + Tests/EditMode/ with dotnet against a minimal NUnit shim and
# runs every [Test] method. Exits non-zero when a case fails.
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet not found on PATH" >&2
  exit 127
fi

dotnet run --project Tools/Verify/Verify.csproj --configuration Release --nologo
status=$?

if [ "$status" -ne 0 ]; then
  echo "verify: FAILED (exit $status)" >&2
else
  echo "verify: OK"
fi

exit "$status"
