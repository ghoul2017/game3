#!/bin/bash
# pre-compact.sh - PreCompact hook: save context before compaction
# Saves current work state to .claude/compact-snapshot.md

cd "$(dirname "$0")/../.." || exit 0

snapshot_file=".claude/compact-snapshot.md"
timestamp=$(date "+%Y-%m-%d %H:%M:%S")

# Get git status
branch=$(git branch --show-current 2>/dev/null || echo "unknown")
last_commit=$(git log -1 --format="%h %s" 2>/dev/null || echo "no commits")
uncommitted=$(git status --porcelain 2>/dev/null)

# Get progress - use grep -o and wc -l for better compatibility
completed=$(grep -o '"passes": true' doc/feature_list.json 2>/dev/null | wc -l | tr -d ' ')
total=$(grep -o '"passes"' doc/feature_list.json 2>/dev/null | wc -l | tr -d ' ')
completed=${completed:-0}
total=${total:-0}

# Write snapshot
cat > "$snapshot_file" << EOF
# Compact Snapshot

**Generated**: $timestamp

## Git State

- **Branch**: $branch
- **Last commit**: $last_commit

## Progress

- **Completed**: $completed/$total features

## Uncommitted Changes

\`\`\`
$uncommitted
\`\`\`

## Notes

This snapshot was created before context compaction.
Review uncommitted changes and recent work to restore context.
EOF

echo "Pre-compact snapshot saved to $snapshot_file"
exit 0
