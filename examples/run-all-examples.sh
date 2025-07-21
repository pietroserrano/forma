#!/bin/bash

echo "=== Running All Forma Console Examples ==="
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

echo "=== All Examples Completed Successfully ==="