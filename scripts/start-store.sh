#!/usr/bin/env bash
# Starts the Waracle Store (API on :4000, web on :5173) the way the test suite expects it.
# Usage: scripts/start-store.sh [path-to-qa-candidate-test]   (default: ../qa-candidate-test)
# Works in bash on macOS/Linux and in Git Bash on Windows. Ctrl+C stops both processes.
set -euo pipefail

APP_DIR="${1:-$(cd "$(dirname "$0")/../.." && pwd)/qa-candidate-test}"
APP_REPO="https://github.com/Waracle/qa-candidate-test.git"

if [ ! -d "$APP_DIR" ]; then
  echo "Cloning $APP_REPO into $APP_DIR"
  git clone "$APP_REPO" "$APP_DIR"
fi

cd "$APP_DIR"

# Install from the monorepo root: the web app's in-browser mock imports bcryptjs, which only
# the backend workspace declares, so a web-only install does not run (see README → Observations).
if [ ! -d node_modules/bcryptjs ]; then
  echo "Installing web + backend dependencies"
  npm ci --workspace web --workspace backend --include-workspace-root --no-audit --no-fund
fi

cleanup() { trap - EXIT INT TERM; kill 0 2>/dev/null || true; }
trap cleanup EXIT INT TERM

echo "Starting API on http://localhost:4000"
npm run start:backend &

echo "Starting web on http://localhost:5173 (linked to the API)"
(cd web && VITE_API_URL=http://localhost:4000 npx vite --port 5173 --strictPort) &

for attempt in $(seq 1 30); do
  if curl -sf http://localhost:4000/api/health > /dev/null && curl -sf http://localhost:5173 > /dev/null; then
    echo "Store is up. Run the suite with: dotnet test"
    wait
    exit 0
  fi
  sleep 2
done

echo "Store did not start within 60 s" >&2
exit 1
