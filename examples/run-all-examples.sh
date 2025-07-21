#!/bin/bash

echo "=== Running All Forma Examples ==="
echo ""

echo "Building all examples first..."
dotnet build
echo ""

echo "=== Console Examples ==="
echo ""

echo "1. Mediator Pattern Example"
echo "----------------------------"
cd console/Forma.Examples.Console.Mediator
dotnet run --no-build
echo ""

echo "2. Decorator Pattern Example"
echo "-----------------------------"
cd ../Forma.Examples.Console.Decorator
dotnet run --no-build
echo ""

echo "3. Chains (Pipeline) Pattern Example"
echo "-------------------------------------"
cd ../Forma.Examples.Console.Chains
dotnet run --no-build
echo ""

echo "4. Complete Integration Example"
echo "-------------------------------"
cd ../Forma.Examples.Console.DependencyInjection
dotnet run --no-build
echo ""

echo "=== Web Examples ==="
echo ""

echo "5. ASP.NET Core Web API Example"
echo "--------------------------------"
echo "Starting web server on background (check console for URL)..."
cd ../../web/Forma.Examples.Web.AspNetCore
echo "To run the web example:"
echo "  cd examples/web/Forma.Examples.Web.AspNetCore && dotnet run"
echo "  Then browse to the URL shown in console + '/swagger'"
echo ""

echo "=== All Examples Ready ==="
echo "Note: Web example requires manual start with 'dotnet run' in the web project directory"