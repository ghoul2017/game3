#!/bin/bash
# validate-cs.sh - PostToolUse/Write,Edit hook: validate new C# files
# Exit 0 = valid, Exit 2 = issues found

file_path="$1"

# Check if file exists
if [ ! -f "$file_path" ]; then
    exit 0
fi

# Only process .cs files
if [[ "$file_path" != *.cs ]]; then
    exit 0
fi

errors=()

# Get the class name from the file
class_name=$(grep -oP '(?<=class )\w+' "$file_path" | head -1)
file_name=$(basename "$file_path" .cs)

# Check 1: Class name matches file name
if [ -n "$class_name" ] && [ "$class_name" != "$file_name" ]; then
    errors+=("Class name '$class_name' does not match file name '$file_name'")
fi

# Check 2: Namespace based on directory
expected_namespace=""
if [[ "$file_path" == *"Scripts/Core"* ]]; then
    expected_namespace="Game.Core"
elif [[ "$file_path" == *"Scripts/UI"* ]]; then
    expected_namespace="Game.UI"
elif [[ "$file_path" == *"Scripts/Game/Battle"* ]]; then
    expected_namespace="Game.Battle"
elif [[ "$file_path" == *"Scripts/Game/Config"* ]]; then
    expected_namespace="Game.Config"
elif [[ "$file_path" == *"Scripts/Game/Data"* ]]; then
    expected_namespace="Game.Data"
elif [[ "$file_path" == *"Scripts/Game"* ]]; then
    expected_namespace="Game"
fi

if [ -n "$expected_namespace" ]; then
    if ! grep -q "namespace $expected_namespace" "$file_path"; then
        errors+=("Expected namespace '$expected_namespace' not found")
    fi
fi

# Check 3: .meta file exists (Unity requirement)
meta_file="${file_path}.meta"
if [ ! -f "$meta_file" ]; then
    errors+=("Missing .meta file for Unity: $meta_file")
fi

# Report errors
if [ ${#errors[@]} -gt 0 ]; then
    echo "WARNING: C# file validation issues for $file_path" >&2
    for err in "${errors[@]}"; do
        echo "  - $err" >&2
    done
    # Only exit 2 for serious issues (namespace mismatch)
    # .meta file is just a warning
    if [[ "${errors[*]}" == *"namespace"* ]]; then
        exit 2
    fi
fi

exit 0
