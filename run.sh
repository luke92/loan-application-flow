#!/usr/bin/env bash
# Runs the backend API, the mock external service, and the frontend together,
# locally (no Docker) — the same three steps described in README.md, started
# in one command. Logs from all three are interleaved in this terminal,
# prefixed by service name. Press Ctrl+C to stop everything.

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$ROOT_DIR/backend"
MOCK_DIR="$ROOT_DIR/mock-external-service"
FRONTEND_DIR="$ROOT_DIR/frontend"

command -v dotnet >/dev/null 2>&1 || { echo "dotnet SDK not found. Install .NET 8+ and try again." >&2; exit 1; }
command -v npm >/dev/null 2>&1 || { echo "npm not found. Install Node.js 18+ and try again." >&2; exit 1; }

if [ ! -d "$MOCK_DIR/node_modules" ]; then
  echo "==> Installing mock-external-service dependencies..."
  (cd "$MOCK_DIR" && npm install)
fi

if [ ! -f "$FRONTEND_DIR/.env.local" ] && [ -f "$FRONTEND_DIR/.env.local.example" ]; then
  cp "$FRONTEND_DIR/.env.local.example" "$FRONTEND_DIR/.env.local"
fi
# If neither file exists, that's fine too: lib/api.ts falls back to
# http://localhost:5080 when NEXT_PUBLIC_API_BASE_URL isn't set.

if [ ! -d "$FRONTEND_DIR/node_modules" ]; then
  echo "==> Installing frontend dependencies..."
  (cd "$FRONTEND_DIR" && npm install)
fi

# Kill every process in this script's process group on exit/Ctrl+C, so the
# three background services never outlive the script.
trap 'echo; echo "Stopping all services..."; kill 0' EXIT INT TERM

echo
echo "==> Backend API      http://localhost:5080"
echo "==> Mock service     http://localhost:4000"
echo "==> Frontend         http://localhost:3000"
echo "Press Ctrl+C to stop everything."
echo

(cd "$BACKEND_DIR" && ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Api --urls http://localhost:5080 2>&1 | sed -u 's/^/[backend]  /') &
(cd "$MOCK_DIR" && npm start 2>&1 | sed -u 's/^/[mock]     /') &
(cd "$FRONTEND_DIR" && npm run dev 2>&1 | sed -u 's/^/[frontend] /') &

wait
