#!/bin/bash
# Script to clean all build artifacts and force a fresh rebuild

echo "Cleaning build artifacts..."

# Remove all bin and obj folders
find /home/user/myFlatLightLogin/src -type d -name "bin" -exec rm -rf {} + 2>/dev/null
find /home/user/myFlatLightLogin/src -type d -name "obj" -exec rm -rf {} + 2>/dev/null

echo "Build artifacts cleaned!"
echo ""
echo "Please rebuild your solution in Visual Studio:"
echo "1. Open Visual Studio"
echo "2. Right-click on the solution"
echo "3. Select 'Clean Solution'"
echo "4. Then select 'Rebuild Solution'"
