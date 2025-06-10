#!/bin/bash

# test-workflow.sh - Script for testing GitHub Actions locally with act

# Function to show script usage
show_usage() {
    echo "Usage: $0 -t <workflow_type> [-v <version>] [-c <component>]"
    echo ""
    echo "Parameters:"
    echo "  -t, --type       Workflow type (core or component)"
    echo "  -v, --version    Version to use for the test (default: 1.0.0-test)"
    echo "  -c, --component  Component for component workflow (default: chains)"
    echo ""
    echo "Examples:"
    echo "  $0 -t core"
    echo "  $0 -t component -c pubsub -v 1.2.3-test"
    exit 1
}

# Analisi dei parametri
WORKFLOW_TYPE=""
VERSION="1.0.0-test"
COMPONENT="chains"

while [[ $# -gt 0 ]]; do
    key="$1"
    case $key in
        -t|--type)
            WORKFLOW_TYPE="$2"
            shift
            shift
            ;;
        -v|--version)
            VERSION="$2"
            shift
            shift
            ;;
        -c|--component)
            COMPONENT="$2"
            shift
            shift
            ;;        *)
            echo "Unrecognized parameter: $1"
            show_usage
            ;;
    esac
done

# Parameter validation
if [ -z "$WORKFLOW_TYPE" ]; then
    echo "Error: The workflow_type parameter is mandatory"
    show_usage
fi

if [ "$WORKFLOW_TYPE" != "core" ] && [ "$WORKFLOW_TYPE" != "component" ]; then
    echo "Error: The workflow type must be 'core' or 'component'"
    show_usage
fi

# Check if Docker is running
if ! docker info &> /dev/null; then
    echo "Error: Docker is not running. Start Docker and try again."
    exit 1
fi

# Check if act is installed
if ! command -v act &> /dev/null; then
    echo "Error: act is not installed. Install act following the instructions in scripts/install-act.md"
    exit 1
fi

# Determine which workflow to test and set parameters
WORKFLOW_FILE=""
EVENT_NAME="push"
EVENT_FILE=$(mktemp)

if [ "$WORKFLOW_TYPE" = "core" ]; then
    WORKFLOW_FILE=".github/workflows/nuget-deploy.yml"
    TAG_NAME="v${VERSION}-core"
    cat > "$EVENT_FILE" <<EOF
{
  "ref": "refs/tags/$TAG_NAME",
  "repository": {
    "name": "forma",
    "owner": {
      "name": "user"
    }
  }
}
EOF
else
    WORKFLOW_FILE=".github/workflows/nuget-component-deploy.yml"
    TAG_NAME="v${VERSION}-${COMPONENT}"
    cat > "$EVENT_FILE" <<EOF
{
  "ref": "refs/tags/$TAG_NAME",
  "repository": {
    "name": "forma",
    "owner": {
      "name": "user"
    }
  }
}
EOF
fi

echo "Testing workflow type '$WORKFLOW_TYPE' with tag '$TAG_NAME'..."

# Run act and simulate a tag push event
act_cmd="act push --eventpath $EVENT_FILE -W $WORKFLOW_FILE --secret NUGET_API_KEY=fake-api-key --container-architecture linux/amd64"

# Show the command
echo "Act command: $act_cmd --dryrun"

# Run act with --dryrun to see what will be executed
eval "$act_cmd --dryrun"

read -p "Do you want to proceed with the actual execution? [y/N] " confirmation
if [[ $confirmation =~ ^[Yy]$ ]]; then
    # Actually run act
    eval "$act_cmd"
else
    echo "Execution cancelled."
fi

# Remove temporary file
if [ -f "$EVENT_FILE" ]; then
    rm "$EVENT_FILE"
fi
