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

tmux new-session -d -s "$SESSION_NAME" -n dev

# 2x2 Pane layout:
#   [0] top-left  = api
#   [1] top-right = client
#   [2] bot-left  = tests
#   [3] bot-right = logs

# Important: target panes explicitly (detached sessions can have surprising "current pane" state).
# Split right (client)
tmux split-window -t "$SESSION_NAME":dev.0 -h
# Split bottom-left (tests)
tmux split-window -t "$SESSION_NAME":dev.0 -v
# Split bottom-right (logs)
tmux split-window -t "$SESSION_NAME":dev.1 -v

# Normalize layout to a clean 2x2 grid
tmux select-layout -t "$SESSION_NAME":dev tiled

# Pane titles (make them visible in pane borders)
tmux set-option -t "$SESSION_NAME":dev pane-border-status top
tmux set-option -t "$SESSION_NAME":dev pane-border-format " #{pane_title} "
tmux select-pane -t "$SESSION_NAME":dev.0 -T "api"
tmux select-pane -t "$SESSION_NAME":dev.1 -T "client"
tmux select-pane -t "$SESSION_NAME":dev.2 -T "tests"
tmux select-pane -t "$SESSION_NAME":dev.3 -T "logs"

# Commands
tmux send-keys -t "$SESSION_NAME":dev.0 "cd \"$API_DIR\" && dotnet watch run" C-m

# Only install deps if node_modules is missing
tmux send-keys -t "$SESSION_NAME":dev.1 "cd \"$CLIENT_DIR\" && if [ ! -d node_modules ]; then npm install; fi && npm run start" C-m

tmux send-keys -t "$SESSION_NAME":dev.2 "cd \"$TESTS_DIR\" && dotnet watch test" C-m

tmux send-keys -t "$SESSION_NAME":dev.3 "cd \"$API_DIR\" && (ls -1 logs 2>/dev/null || true); tail -F logs/*.log 2>/dev/null || echo 'No log files yet (waiting for first write)...'" C-m

tmux select-pane -t "$SESSION_NAME":dev.0
tmux attach -t "$SESSION_NAME"
