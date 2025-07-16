#!/bin/bash

# test-workf# Function to show script usage
show_usage() {
    echo "Usage: $0 -t <workflow_type> [-v <version>] [-c <component>] [-r <release_type>] [-b <branch>] [--local-nuget] [--nuget-container <n>] [--nuget-port <port>]".sh - Script for testing GitHub Actions locally with act
#
# Questo script è stato aggiornato per supportare i nuovi workflow basati su Nerdbank.GitVersioning
# che utilizzano i branch invece dei tag per determinare il tipo di release.
#
# Examples:
#   # Test core package deployment workflow with default settings (preview release)
#   ./test-workflow.sh -t core
#
#   # Test core package deployment workflow for stable release
#   ./test-workflow.sh -t core -r stable
#
#   # Test core package deployment workflow simulating a branch push
#   ./test-workflow.sh -t core -b "v1.0"            # For stable release branch
#   ./test-workflow.sh -t core -b "release/v1.0"    # For preview release branch
#
#   # Test component package deployment workflow (manual trigger only)
#   ./test-workflow.sh -t component -c chains
#
#   # Test with local NuGet server
#   ./test-workflow.sh -t core --local-nuget
#
#   # Test with custom local NuGet server settings
#   ./test-workflow.sh -t component -c chains --local-nuget --nuget-port 5000

# Function to show script usage
show_usage() {
    echo "Usage: $0 -t <workflow_type> [-v <version>] [-c <component>] [--local-nuget] [--nuget-container <name>] [--nuget-port <port>]"
    echo ""    echo "Parameters:"
    echo "  -t, --type             Workflow type (core or component)"
    echo "  -v, --version          Version to use for the test (default: 1.0.0-test)"
    echo "  -c, --component        Component for component workflow (default: chains)"
    echo "  -r, --release-type     Release type (preview or stable, default: preview)"
    echo "  -b, --branch           Simulate a branch push (e.g. v1.0 or release/v1.0)"
    echo "  --local-nuget          Use local NuGet server in Docker"
    echo "  --nuget-container      Name for the NuGet server container (default: local-nuget-server)"
    echo "  --nuget-port           Port for the NuGet server (default: 5555)"
    echo ""    echo "Examples:"
    echo "  $0 -t core                                    # Test core workflow with preview release"
    echo "  $0 -t core -r stable                          # Test core workflow with stable release"
    echo "  $0 -t core -b \"v1.0\"                         # Test core workflow simulating v1.0 branch push"
    echo "  $0 -t core -b \"release/v1.0\"                 # Test core workflow simulating release/v1.0 branch push"
    echo "  $0 -t component -c chains                     # Test component workflow for chains"
    echo "  $0 -t core --local-nuget                      # Test using local NuGet server"
    echo "  $0 -t component -c chains --local-nuget --nuget-port 5000"
    exit 1
}

# Analisi dei parametri
WORKFLOW_TYPE=""
VERSION="1.0.0-test"
COMPONENT="chains"
RELEASE_TYPE="preview"
SIMULATE_BRANCH=""
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
        -r|--release-type)
            RELEASE_TYPE="$2"
            shift
            shift
            ;;
        -b|--branch)
            SIMULATE_BRANCH="$2"
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

if [ "$RELEASE_TYPE" != "preview" ] && [ "$RELEASE_TYPE" != "stable" ]; then
    echo "Error: The release type must be 'preview' or 'stable'"
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
EVENT_NAME="workflow_dispatch"
EVENT_FILE=$(mktemp)
TEMP_WORKFLOW_FILE=""

if [ "$WORKFLOW_TYPE" = "core" ]; then
    WORKFLOW_FILE=".github/workflows/nuget-deploy.yml"
else
    WORKFLOW_FILE=".github/workflows/nuget-component-deploy.yml"
fi

# Create event JSON based on workflow type and simulation parameters
if [ -n "$SIMULATE_BRANCH" ]; then
    EVENT_NAME="push"
    # Simulate branch push event
    cat > "$EVENT_FILE" <<EOF
{
  "ref": "refs/heads/$SIMULATE_BRANCH",
  "repository": {
    "name": "forma",
    "owner": {
      "name": "user"
    }
  }
}
EOF
else
    # Use workflow_dispatch event
    cat > "$EVENT_FILE" <<EOF
{
  "inputs": {
    "releaseType": "$RELEASE_TYPE"
  },
  "repository": {
    "name": "forma",
    "owner": {
      "name": "user"
    }
  }
}
EOF
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

# Se stiamo testando il workflow dei component, dobbiamo simulare il job check-core-packages
if [ "$WORKFLOW_TYPE" = "component" ] && [ -z "$TEMP_WORKFLOW_FILE" ]; then
    TEMP_DIR=$(mktemp -d)
    TEMP_WORKFLOW_FILE="${TEMP_DIR}/$(basename $WORKFLOW_FILE)"
    
    # Read original workflow
    cat "$WORKFLOW_FILE" | sed '/jobs:/a\
  # Job semplificato per test locale\
  check-core-packages:\
    runs-on: ubuntu-latest\
    steps:\
      - name: Mock Check Core Packages\
        run: |\
          echo "Simulating successful check of core packages..."\
          exit 0\
' > "$TEMP_WORKFLOW_FILE"
    
    WORKFLOW_FILE="$TEMP_WORKFLOW_FILE"
    echo -e "\e[36mAdded mock job for component dependency checks\e[0m"
fi

# Build appropriate message based on simulation type
if [ -n "$SIMULATE_BRANCH" ]; then
    echo -e "\e[33mTesting workflow type '$WORKFLOW_TYPE' simulating push to branch '$SIMULATE_BRANCH'...\e[0m"
elif [ "$WORKFLOW_TYPE" = "core" ]; then
    echo -e "\e[33mTesting '$WORKFLOW_TYPE' workflow with releaseType '$RELEASE_TYPE'...\e[0m"
else
    echo -e "\e[33mTesting '$WORKFLOW_TYPE' workflow for component '$COMPONENT' with releaseType '$RELEASE_TYPE'...\e[0m"
fi

# Run act and simulate the event
act_cmd="act $EVENT_NAME --eventpath $EVENT_FILE -W $WORKFLOW_FILE --secret NUGET_API_KEY=$NUGET_API_KEY --container-architecture linux/amd64"

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
    if [ -n "$TEMP_DIR" ] && [ -d "$TEMP_DIR" ]; then
        rm -rf "$TEMP_DIR"
    fi
    
    # Handle NuGet server if applicable
    if [ "$USE_LOCAL_NUGET" = true ] && [ "$LOCAL_NUGET_RUNNING" = true ]; then
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
