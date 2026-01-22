#!/bin/bash

# RunASP.NET Web Deploy via Docker
# This script uses a Windows container to run msdeploy

set -e

echo "🚀 Deploying to RunASP.NET using Web Deploy..."
echo "================================================"
echo ""

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed."
    echo ""
    echo "Install Docker Desktop from: https://www.docker.com/products/docker-desktop"
    echo "Or use manual deployment:"
    echo "  1. Upload qalam-deployment.zip to RunASP.NET control panel"
    echo "  2. Extract it in the File Manager"
    exit 1
fi

echo "📦 Creating deployment package..."

# Create Web Deploy package
cd publish
zip -rq ../deploy-package.zip .
cd ..

echo "✅ Package created: deploy-package.zip"
echo ""
echo "🐳 Running Web Deploy in Docker container..."
echo ""

# Run msdeploy in Windows container
docker run --rm \
    -v "$(pwd):/workspace" \
    -w /workspace \
    mcr.microsoft.com/dotnet/framework/sdk:4.8 \
    powershell -Command "
        Write-Host 'Installing Web Deploy...'
        
        # Download and install Web Deploy
        \$url = 'https://download.microsoft.com/download/0/1/D/01DC28EA-638C-4A22-A57B-4CEF97755C6C/WebDeploy_amd64_en-US.msi'
        \$output = 'webdeploy.msi'
        
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri \$url -OutFile \$output
        
        Start-Process msiexec.exe -ArgumentList '/i', 'webdeploy.msi', '/qn', '/norestart' -Wait
        
        # Deploy using msdeploy
        & 'C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe' \
            -verb:sync \
            -source:package='deploy-package.zip' \
            -dest:auto,computerName='https://site49539.siteasp.net:8172/msdeploy.axd?site=site49539',userName='site49539',password='nG%3!8JjN?x5',authtype='Basic' \
            -allowUntrusted \
            -verbose
    "

echo ""
echo "================================================"
echo "✅ Deployment completed!"
echo "🌐 Visit: http://qalam.runasp.net/"
echo ""
