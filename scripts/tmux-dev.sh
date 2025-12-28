#!/usr/bin/env bash
set -euo pipefail

SESSION_NAME=${1:-speakstore}
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

API_DIR="$ROOT_DIR/SpeakStoreLocate.ApiService"
CLIENT_DIR="$ROOT_DIR/SpeakStoreLocate.Client"
TESTS_DIR="$ROOT_DIR/SpeakStoreLocate.Tests"

if ! command -v tmux >/dev/null 2>&1; then
  echo "tmux is not installed. Install it first (e.g. apt install tmux)." >&2
  exit 1
fi

if tmux has-session -t "$SESSION_NAME" 2>/dev/null; then
  echo "Session '$SESSION_NAME' already exists. Attaching..."
  tmux attach -t "$SESSION_NAME"
  exit 0
fi

tmux new-session -d -s "$SESSION_NAME" -n api

tmux send-keys -t "$SESSION_NAME":api "cd \"$API_DIR\" && dotnet watch run" C-m

tmux new-window -t "$SESSION_NAME" -n client
# Only install deps if node_modules is missing

tmux send-keys -t "$SESSION_NAME":client "cd \"$CLIENT_DIR\" && if [ ! -d node_modules ]; then npm install; fi && npm run start" C-m

tmux new-window -t "$SESSION_NAME" -n tests

tmux send-keys -t "$SESSION_NAME":tests "cd \"$TESTS_DIR\" && dotnet watch test" C-m

tmux new-window -t "$SESSION_NAME" -n logs

tmux send-keys -t "$SESSION_NAME":logs "cd \"$API_DIR\" && (ls -1 logs 2>/dev/null || true); tail -F logs/*.log 2>/dev/null || echo 'No log files yet (waiting for first write)...'" C-m

tmux select-window -t "$SESSION_NAME":api

tmux attach -t "$SESSION_NAME"
