#!/bin/sh

# Inject runtime environment variables into assets/env.js
# This allows the Angular app to read the API_BASE from environment at runtime

API_BASE="${API_BASE:-}"

if [ -n "$API_BASE" ]; then
    cat > /usr/share/nginx/html/assets/env.js <<EOF
window.__env = window.__env || {};
window.__env.apiBase = '$API_BASE';
EOF
    echo "ℹ️  Injected API_BASE: $API_BASE"
else
    echo "⚠️  API_BASE not set; using compiled defaults"
fi

# Execute the command passed to entrypoint (typically nginx)
exec "$@"
