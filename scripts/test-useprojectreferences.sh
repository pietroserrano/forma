#!/bin/bash

# test-useprojectreferences.sh - Script to test UseProjectReferences behavior
# This script verifies that the test project can work correctly with both
# UseProjectReferences=true (local development) and UseProjectReferences=false (GitHub Actions)

echo "Testing UseProjectReferences behavior..."
echo

# Test with UseProjectReferences=true (default local development setting)
echo "=== Testing with UseProjectReferences=true (local development mode) ==="
echo "Running: dotnet build tests/Forma.Tests/Forma.Tests.csproj -p:UseProjectReferences=true"
if dotnet build tests/Forma.Tests/Forma.Tests.csproj -p:UseProjectReferences=true --verbosity minimal; then
    echo "✅ Build successful with UseProjectReferences=true"
else
    echo "❌ Build failed with UseProjectReferences=true"
    echo "This indicates an issue with project references."
fi
echo

# Test with UseProjectReferences=false (GitHub Actions mode)
echo "=== Testing with UseProjectReferences=false (GitHub Actions mode) ==="
echo "Running: dotnet build tests/Forma.Tests/Forma.Tests.csproj -p:UseProjectReferences=false"
if dotnet build tests/Forma.Tests/Forma.Tests.csproj -p:UseProjectReferences=false --verbosity minimal; then
    echo "✅ Build successful with UseProjectReferences=false"
    echo "This confirms the project can use NuGet package references instead of project references."
else
    echo "❌ Build failed with UseProjectReferences=false"
    echo "This indicates the NuGet packages may not be available or there's a configuration issue."
    echo "Note: This is expected in a fresh environment where NuGet packages haven't been published yet."
fi
echo

# Test building a single project to simulate GitHub Actions pack step
echo "=== Testing project packing with UseProjectReferences=false ==="
echo "Running: dotnet pack src/Forma.Core/Forma.Core.csproj -p:UseProjectReferences=false"
if dotnet pack src/Forma.Core/Forma.Core.csproj -p:UseProjectReferences=false -o packages --verbosity minimal; then
    echo "✅ Pack successful with UseProjectReferences=false"
    echo "This confirms projects can be packaged without project references."
    
    # Show generated packages
    if [ -d "packages" ]; then
        echo "Generated packages:"
        ls -la packages/
    fi
else
    echo "❌ Pack failed with UseProjectReferences=false"
fi
echo

echo "=== Summary ==="
echo "The UseProjectReferences property controls whether projects use:"
echo "  • UseProjectReferences=true:  Local project references (for development)"
echo "  • UseProjectReferences=false: NuGet package references (for CI/CD)"
echo
echo "This design allows:"
echo "  1. Local development with immediate changes between projects"
echo "  2. CI/CD workflows that package each project independently"
echo "  3. Tests that can run against either project references or NuGet packages"
echo
echo "GitHub Actions uses UseProjectReferences=false to ensure clean package builds."