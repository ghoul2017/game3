#!/bin/bash
# session-start.sh - SessionStart hook: validate core files and report progress
# Exit 0 = OK, Exit 2 = critical files missing

cd "$(dirname "$0")/../.." || exit 2

errors=()

# Check 1: feature_list.json exists
if [ ! -f "doc/feature_list.json" ]; then
    errors+=("Missing doc/feature_list.json")
fi

# Check 2: progress.txt exists
if [ ! -f "doc/progress.txt" ]; then
    errors+=("Missing doc/progress.txt")
fi

# Check 3: CLAUDE.md exists
if [ ! -f "CLAUDE.md" ]; then
    errors+=("Missing CLAUDE.md")
fi

# Report errors if any
if [ ${#errors[@]} -gt 0 ]; then
    echo "SESSION START: Critical files missing!" >&2
    for err in "${errors[@]}"; do
        echo "  - $err" >&2
    done
    exit 2
fi

# Report progress - use grep output more carefully
total_features=$(grep -o '"passes"' doc/feature_list.json 2>/dev/null | wc -l | tr -d ' ')
completed_features=$(grep -o '"passes": true' doc/feature_list.json 2>/dev/null | wc -l | tr -d ' ')

# Ensure we have valid numbers
total_features=${total_features:-0}
completed_features=${completed_features:-0}
pending_features=$((total_features - completed_features))

# Get current git branch
branch=$(git branch --show-current 2>/dev/null || echo "unknown")

# Get last commit
last_commit=$(git log -1 --format="%h %s" 2>/dev/null || echo "no commits")

# Get uncommitted changes
changes=$(git status --porcelain 2>/dev/null | wc -l | tr -d ' ')
changes=${changes:-0}

echo ""
echo "=========================================="
echo "         GAME3 SESSION START"
echo "=========================================="
echo "Branch: $branch"
echo "Progress: $completed_features/$total_features features done"
if [ "$pending_features" -gt 0 ]; then
    echo "Pending: $pending_features features remaining"
else
    echo "Status: All features completed!"
fi
echo "Last commit: $last_commit"
if [ "$changes" -gt 0 ]; then
    echo "Uncommitted: $changes files"
fi
echo "=========================================="
echo ""

exit 0
