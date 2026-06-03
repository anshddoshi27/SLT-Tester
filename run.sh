#!/usr/bin/env bash
set -euo pipefail

export PATH="${HOME}/.dotnet:${PATH:-}"
export DOTNET_ROOT="${HOME}/.dotnet"

ROOT="$(cd "$(dirname "$0")" && pwd)"
API="${ROOT}/src/SltVirtualTest.Api"
CLIENT="${ROOT}/src/SltVirtualTest.Client"
API_URL="http://localhost:5295"

api_pid=""

cleanup() {
  if [[ -n "${api_pid}" ]] && kill -0 "${api_pid}" 2>/dev/null; then
    echo ""
    echo "Stopping API (PID ${api_pid})..."
    kill "${api_pid}" 2>/dev/null || true
    wait "${api_pid}" 2>/dev/null || true
  fi
}
trap cleanup EXIT INT TERM

echo "Starting API..."
dotnet run --project "${API}" --launch-profile http &
api_pid=$!

echo "Waiting for API at ${API_URL}..."
for _ in $(seq 1 60); do
  if curl -sf "${API_URL}/swagger/index.html" >/dev/null 2>&1; then
    echo "API ready."
    break
  fi
  if ! kill -0 "${api_pid}" 2>/dev/null; then
    echo "API process exited unexpectedly."
    exit 1
  fi
  sleep 1
done

echo "Starting Blazor client (Ctrl+C stops both)..."
dotnet run --project "${CLIENT}"
