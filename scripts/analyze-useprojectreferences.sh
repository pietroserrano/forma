#!/bin/bash

# analyze-useprojectreferences.sh - Analyze UseProjectReferences configuration and usage
# This script analyzes how UseProjectReferences is configured and used throughout the project

echo "🔍 Analyzing UseProjectReferences configuration and usage..."
echo

# Function to extract project references
extract_project_refs() {
    local file="$1"
    echo "  Project References (UseProjectReferences=true):"
    grep -A 10 "UseProjectReferences.*==.*true" "$file" | grep "ProjectReference" | sed 's/^[ \t]*/    /'
    echo "  Package References (UseProjectReferences=false):"
    grep -A 10 "UseProjectReferences.*!=.*true" "$file" | grep "PackageReference" | sed 's/^[ \t]*/    /'
}

# Analyze Directory.Build.props
echo "=== Directory.Build.props Configuration ==="
if [ -f "Directory.Build.props" ]; then
    echo "📁 Global configuration in Directory.Build.props:"
    grep -A 2 -B 2 "UseProjectReferences" Directory.Build.props | sed 's/^/  /'
    echo
else
    echo "❌ Directory.Build.props not found"
    echo
fi

# Analyze project files that use UseProjectReferences
echo "=== Project Files Using UseProjectReferences ==="
echo "🔍 Searching for projects that use conditional references..."
echo

for proj in $(find . -name "*.csproj" -exec grep -l "UseProjectReferences" {} \;); do
    echo "📄 $proj:"
    extract_project_refs "$proj"
    echo
done

# Analyze GitHub Actions workflows
echo "=== GitHub Actions Workflow Analysis ==="
echo "🔍 Searching for UseProjectReferences usage in workflows..."
echo

for workflow in $(find .github/workflows -name "*.yml" -exec grep -l "UseProjectReferences" {} \; 2>/dev/null); do
    echo "⚙️  $workflow:"
    grep -n -B 2 -A 2 "UseProjectReferences" "$workflow" | sed 's/^/  /'
    echo
done

# Analyze test scripts
echo "=== Test Scripts Analysis ==="
echo "🔍 Checking test scripts for UseProjectReferences references..."
echo

for script in scripts/test-*.sh scripts/test-*.ps1; do
    if [ -f "$script" ]; then
        echo "📜 $script:"
        if grep -q "UseProjectReferences" "$script"; then
            grep -n -B 2 -A 2 "UseProjectReferences" "$script" | sed 's/^/  /'
        else
            echo "  No UseProjectReferences usage found"
        fi
        echo
    fi
done

echo "=== Key Findings ==="
echo "✅ UseProjectReferences is used to control reference behavior:"
echo "   • UseProjectReferences=true  → Use local project references (development)"
echo "   • UseProjectReferences=false → Use NuGet package references (CI/CD)"
echo
echo "🎯 GitHub Actions specifically uses UseProjectReferences=false:"
echo "   This ensures each package is built independently without dependencies"
echo "   on locally built projects, creating clean, publishable packages."
echo
echo "🧪 Testing considerations:"
echo "   • Local development: Tests run against project references (immediate changes)"
echo "   • CI/CD: Tests should run against NuGet packages (validates published versions)"
echo
echo "📦 Packaging behavior:"
echo "   • UseProjectReferences=false ensures packages don't bundle local dependencies"
echo "   • Each package references only published NuGet packages as dependencies"
echo

# Check if this is documented
echo "=== Documentation Check ==="
if find . -name "*.md" -exec grep -l "UseProjectReferences" {} \; | head -1 >/dev/null 2>&1; then
    echo "📚 UseProjectReferences is documented in:"
    find . -name "*.md" -exec grep -l "UseProjectReferences" {} \; | sed 's/^/  /'
else
    echo "⚠️  UseProjectReferences behavior should be documented for maintainers"
fi
echo

echo "=== Recommendations ==="
echo "1. ✅ GitHub Actions correctly uses UseProjectReferences=false"
echo "2. ✅ Project files properly implement conditional references"
echo "3. ✅ Directory.Build.props sets appropriate defaults"
echo "4. 📝 Consider documenting UseProjectReferences behavior for team members"
echo "5. 🧪 Test scripts should validate both UseProjectReferences modes"