#!/bin/bash
# validate-commit.sh - PreToolUse/Bash hook: validate commit message format
# Exit 0 = valid, Exit 2 = invalid (blocks the commit)

# Get the command
if [ $# -gt 0 ]; then
    cmd="$*"
elif [ ! -t 0 ]; then
    cmd=$(cat)
else
    exit 0
fi

# Only trigger on actual git commit commands
# Must start with git or have git as the first word
first_word=$(echo "$cmd" | awk '{print $1}')

case "$first_word" in
    git)
        # Check if it's a commit command
        second_word=$(echo "$cmd" | awk '{print $2}')
        if [ "$second_word" != "commit" ]; then
            exit 0
        fi
        ;;
    *)
        # Not a git command, allow
        exit 0
        ;;
esac

# Now we know it's a git commit command
# Check if it has -m flag
if ! echo "$cmd" | grep -q '\-m'; then
    # No -m flag (interactive commit), allow
    exit 0
fi

# Extract message: everything after -m
# Use awk to get everything after -m
msg=$(echo "$cmd" | sed 's/.*-m[[:space:]]*//' | head -1)

# Remove surrounding quotes if present
msg="${msg#\"}"
msg="${msg#'}"
msg="${msg%\"}"
msg="${msg%'}"

# If empty, allow
if [ -z "$msg" ]; then
    exit 0
fi

# Check for valid prefixes (case insensitive)
msg_lower=$(echo "$msg" | tr '[:upper:]' '[:lower:]')

case "$msg_lower" in
    feat:*) exit 0 ;;
    fix:*) exit 0 ;;
    progress:*) exit 0 ;;
    plan:*) exit 0 ;;
    refactor:*) exit 0 ;;
    docs:*) exit 0 ;;
    test:*) exit 0 ;;
    chore:*) exit 0 ;;
    init:*) exit 0 ;;
    ui:*) exit 0 ;;
esac

cat >&2 << 'EOF'

ERROR: Invalid commit message format
EOF
echo "Message: $msg" >&2
cat >&2 << 'EOF'

Valid prefixes: feat, fix, progress, plan, refactor, docs, test, chore, init, ui
Example: feat: [F001] Add new feature
EOF
exit 2
