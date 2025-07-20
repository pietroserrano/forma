#!/bin/bash

# test-workflow.sh - Script for testing GitHub Actions locally with act
#
# Examples:
#   # Test core package deployment workflow with default settings
#   ./test-workflow.sh -t core
#
#   # Test component package deployment workflow for a specific component
#   ./test-workflow.sh -t component -c pubsub -v 1.2.3-test
#
#   # Test with local NuGet server
#   ./test-workflow.sh -t core --local-nuget
#
#   # Test with custom local NuGet server settings
#   ./test-workflow.sh -t component -c pubsub --local-nuget --nuget-port 5000

# Function to show script usage
show_usage() {
    echo "Usage: $0 -t <workflow_type> [-v <version>] [-c <component>] [--local-nuget] [--nuget-container <name>] [--nuget-port <port>]"
    echo ""
    echo "Parameters:"
    echo "  -t, --type             Workflow type (core or component)"
    echo "  -v, --version          Version to use for the test (default: 1.0.0-test)"
    echo "  -c, --component        Component for component workflow (chains, pubsub, mediator, decorator - default: chains)"
    echo "  --local-nuget          Use local NuGet server in Docker"
    echo "  --nuget-container      Name for the NuGet server container (default: local-nuget-server)"
    echo "  --nuget-port           Port for the NuGet server (default: 5555)"
    echo ""
    echo "Examples:"
    echo "  $0 -t core"
    echo "  $0 -t component -c pubsub -v 1.2.3-test"
    echo "  $0 -t component -c chains"
    echo "  $0 -t component -c mediator"  
    echo "  $0 -t component -c decorator"
    echo "  $0 -t core --local-nuget"
    echo "  $0 -t component -c chains --local-nuget --nuget-port 5000"
    exit 1
}

# Analisi dei parametri
WORKFLOW_TYPE=""
VERSION="1.0.0-test"
COMPONENT="chains"
USE_LOCAL_NUGET=false
NUGET_CONTAINER_NAME="local-nuget-server"
NUGET_SERVER_PORT="5555"

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
            ;;
        --local-nuget)
            USE_LOCAL_NUGET=true
            shift
            ;;
        --nuget-container)
            NUGET_CONTAINER_NAME="$2"
            shift
            shift
            ;;
        --nuget-port)
            NUGET_SERVER_PORT="$2"
            shift
            shift
            ;;
        *)
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

# Function to start local NuGet server using Docker
start_local_nuget_server() {
    local container_name=$1
    local port=$2
    local is_running=false
    
    # Check if container already exists
    if docker ps -a --filter name=$container_name --format "{{.Names}}" | grep -q "$container_name"; then
        echo -e "\e[33mNuGet server container '$container_name' already exists.\e[0m"
        
        # Check if it's running
        if docker ps --filter name=$container_name --format "{{.Names}}" | grep -q "$container_name"; then
            echo -e "\e[32mNuGet server is already running.\e[0m"
            is_running=true
        else
            echo -e "\e[36mStarting existing NuGet server container...\e[0m"
            docker start $container_name
            is_running=true
        fi
    else
        echo -e "\e[36mCreating and starting NuGet server container...\e[0m"
        
        # Create volume for persistent storage
        local volume_name="$container_name-data"
        if ! docker volume ls --filter name=$volume_name --format "{{.Name}}" | grep -q "$volume_name"; then
            docker volume create $volume_name
        fi
        
        # Start BaGet NuGet server
        # BaGet is a lightweight NuGet server implementation
        docker run -d --name $container_name \
            -p "${port}:80" \
            -e ApiKey=TEST-API-KEY \
            -v "${volume_name}:/var/baget" \
            --restart unless-stopped \
            loicsharma/baget:latest
            
        # Give the container a moment to start up
        sleep 3
        is_running=true
    fi
    
    # Verify the container is running
    if ! docker ps --filter name=$container_name --format "{{.Names}}" | grep -q "$container_name"; then
        echo -e "\e[31mFailed to start NuGet server container.\e[0m"
        return 1
    fi
    
    echo -e "\e[32mLocal NuGet server is running at http://localhost:$port\e[0m"
    echo -e "\e[32mYou can push packages with API key: TEST-API-KEY\e[0m"
    return 0
}

# Function to stop NuGet server
stop_local_nuget_server() {
    local container_name=$1
    local remove=${2:-false}
    
    if docker ps -a --filter name=$container_name --format "{{.Names}}" | grep -q "$container_name"; then
        if [ "$remove" = true ]; then
            echo -e "\e[33mRemoving NuGet server container...\e[0m"
            docker rm -f $container_name
        else
            echo -e "\e[33mStopping NuGet server container...\e[0m"
            docker stop $container_name
        fi
    fi
}

# Determine which workflow to test and set parameters
WORKFLOW_FILE=""
EVENT_NAME="push"
EVENT_FILE=$(mktemp)
TEMP_WORKFLOW_FILE=""

if [ "$WORKFLOW_TYPE" = "core" ]; then
    WORKFLOW_FILE=".github/workflows/release-core.yml"
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
elif [ "$COMPONENT" = "chains" ]; then
    WORKFLOW_FILE=".github/workflows/release-chains.yml"
    TAG_NAME="v${VERSION}-chains"
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
elif [ "$COMPONENT" = "pubsub" ]; then
    WORKFLOW_FILE=".github/workflows/release-pubsub.yml"
    TAG_NAME="v${VERSION}-pubsub"
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
elif [ "$COMPONENT" = "mediator" ]; then
    WORKFLOW_FILE=".github/workflows/release-mediator.yml"
    TAG_NAME="v${VERSION}-mediator"
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
elif [ "$COMPONENT" = "decorator" ]; then
    WORKFLOW_FILE=".github/workflows/release-decorator.yml"
    TAG_NAME="v${VERSION}-decorator"
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
    echo "Error: Unknown component '$COMPONENT'. Valid components are: chains, pubsub, mediator, decorator"
    exit 1
fi

# Handle local NuGet server if requested
LOCAL_NUGET_RUNNING=false
NUGET_SOURCE_URL="https://api.nuget.org/v3/index.json"
NUGET_API_KEY="fake-api-key"

if [ "$USE_LOCAL_NUGET" = true ]; then
    if start_local_nuget_server "$NUGET_CONTAINER_NAME" "$NUGET_SERVER_PORT"; then
        LOCAL_NUGET_RUNNING=true
        NUGET_SOURCE_URL="http://localhost:${NUGET_SERVER_PORT}/v3/index.json"
        NUGET_API_KEY="TEST-API-KEY"
        
        # Create a temporary workflow file with the modified NuGet source
        TEMP_DIR=$(mktemp -d)
        TEMP_WORKFLOW_FILE="${TEMP_DIR}/$(basename $WORKFLOW_FILE)"
        
        # Read original workflow and modify it for local NuGet server
        sed "s|https://api.nuget.org/v3/index.json|$NUGET_SOURCE_URL|g" "$WORKFLOW_FILE" > "$TEMP_WORKFLOW_FILE"
        
        # Add notification about using local NuGet server
        sed -i "s|name: Push to NuGet|name: Push to Local NuGet Server\n      run: |\n        echo \"Publishing to local NuGet server at $NUGET_SOURCE_URL\"|g" "$TEMP_WORKFLOW_FILE"
        
        # Update workflow file path
        WORKFLOW_FILE="$TEMP_WORKFLOW_FILE"
        
        echo -e "\e[36mUsing local NuGet server at $NUGET_SOURCE_URL with API key: $NUGET_API_KEY\e[0m"
    else
        echo -e "\e[33mWarning: Failed to start local NuGet server. Falling back to fake NuGet publishing.\e[0m"
    fi
fi

echo -e "\e[33mTesting workflow type '$WORKFLOW_TYPE' with tag '$TAG_NAME'...\e[0m"

# Run act and simulate a tag push event
act_cmd="act push --eventpath $EVENT_FILE -W $WORKFLOW_FILE --secret NUGET_API_KEY=$NUGET_API_KEY --container-architecture linux/amd64"

# Show the command
echo -e "\e[36mAct command: $act_cmd --dryrun\e[0m"

# Run act with --dryrun to see what will be executed
eval "$act_cmd --dryrun"

read -p "Do you want to proceed with the actual execution? [y/N] " confirmation
if [[ $confirmation =~ ^[Yy]$ ]]; then
    # Actually run act
    eval "$act_cmd"
else
    echo "Execution cancelled."
fi

# Cleanup resources
function cleanup_resources {
    # Remove temporary files
    if [ -f "$EVENT_FILE" ]; then
        rm "$EVENT_FILE"
    fi
    
    # Cleanup temp workflow file if created
    if [ "$USE_LOCAL_NUGET" = true ] && [ "$LOCAL_NUGET_RUNNING" = true ] && [ -n "$TEMP_DIR" ]; then
        if [ -d "$TEMP_DIR" ]; then
            rm -rf "$TEMP_DIR"
        fi
        
        # Ask if user wants to stop the NuGet server
        read -p "Do you want to stop the local NuGet server? [y/N] " stop_server
        if [[ $stop_server =~ ^[Yy]$ ]]; then
            stop_local_nuget_server "$NUGET_CONTAINER_NAME"
        else
            echo -e "\e[32mNuGet server is still running at http://localhost:$NUGET_SERVER_PORT\e[0m"
            echo -e "\e[33mYou can manage the container manually with 'docker stop/start/rm $NUGET_CONTAINER_NAME'\e[0m"
        fi
    fi
}

# Call cleanup function
cleanup_resources
