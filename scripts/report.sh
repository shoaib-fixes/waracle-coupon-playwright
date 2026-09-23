#!/usr/bin/env bash
# Builds the Allure report from the last `dotnet test` run and opens it in the browser.
# Requires Node.js (Allure 3 is a Node package; no Java needed).
# Usage: scripts/report.sh [Debug|Release]
set -euo pipefail

CONFIG="${1:-Debug}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
RESULTS="$ROOT/src/WaracleStore.CouponTests/bin/$CONFIG/net10.0/allure-results"
OUTPUT="$ROOT/allure-report"

if [ ! -d "$RESULTS" ]; then
  echo "No results at $RESULTS. Run 'dotnet test' first." >&2
  exit 1
fi

# Run from the repo root so allurerc.mjs (grouping + known-issue rules) is picked up.
cd "$ROOT"
npx --yes allure@3 generate "$RESULTS" -o "$OUTPUT" --open
