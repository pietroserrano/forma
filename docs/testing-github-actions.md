# Testing GitHub Actions Locally

This document explains how to test Forma's GitHub Actions workflows locally before pushing to the repository.

## Prerequisites

1. **Docker**: Make sure Docker is installed and running on your system
   - [Download Docker Desktop](https://www.docker.com/products/docker-desktop)

2. **act**: A tool for running GitHub Actions locally
   - **Windows**: `winget install nektos.act`
   - **macOS/Linux**: `brew install act`
   - **Linux (alternative)**: `curl -s https://raw.githubusercontent.com/nektos/act/master/install.sh | sudo bash`

## Using the Test Scripts

We've created scripts to simplify local workflow testing:

### PowerShell (Windows)

```powershell
# Test the core release workflow (Forma.Core, Forma.Mediator, Forma.Decorator)
.\scripts\test-workflow.ps1 -WorkflowType core -Version 2.0.0-test

# Test a specific component release workflow
.\scripts\test-workflow.ps1 -WorkflowType component -Component chains -Version 1.3.0-test
.\scripts\test-workflow.ps1 -WorkflowType component -Component pubsub -Version 1.1.0-test
```

### Bash (Linux/macOS)

```bash
# Make the script executable
chmod +x ./scripts/test-workflow.sh

# Test the core release workflow
./scripts/test-workflow.sh -t core -v 2.0.0-test

# Test a specific component release workflow
./scripts/test-workflow.sh -t component -c chains -v 1.3.0-test
./scripts/test-workflow.sh -t component -c pubsub -v 1.1.0-test
```

## How It Works

The scripts perform the following operations:

1. Verify that Docker is running
2. Verify that `act` is installed
3. Create a temporary event file that simulates a workflow dispatch
4. Run the workflow with `--dry-run` to show what would happen
5. Ask for confirmation before actually running the workflow
6. Run the workflow in a Docker container

## Limitations

- `act` uses Docker images to simulate GitHub Actions environments
- Some aspects of the official GitHub runners may differ from the local environment
- Actions that require access to GitHub APIs may require authentication tokens
- Git operations (tag creation) and GitHub CLI operations (release creation) may not work in the containerized environment
- The new automated release features (Git tags and GitHub releases) are best tested in actual CI/CD environments

## What Gets Tested

The scripts test the core workflow functionality including:
- NuGet package building and publishing
- Version detection using Nerdbank.GitVersioning
- Workflow job dependencies and conditions

The following features require actual GitHub environment:
- Git tag creation and pushing
- GitHub release creation
- Repository permissions and authentication

## Troubleshooting

If you encounter issues:

1. Make sure Docker is running
2. Verify that `act` is installed correctly
3. For Docker image issues, try `act -P ubuntu-latest=nektos/act-environments-ubuntu:18.04`
4. For more memory: `act --container-options="-e DOCKER_OPTS=--memory=4g"`
