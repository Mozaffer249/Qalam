#!/bin/bash

# Simple deployment to RunASP.NET
# This uploads the deployment ZIP to be manually extracted

set -e

echo "🚀 RunASP.NET Deployment Helper"
echo "================================================"
echo ""

# Check if ZIP exists
if [ ! -f "qalam-deployment.zip" ]; then
    echo "❌ Error: qalam-deployment.zip not found!"
    echo "The deployment package should be in the current directory."
    exit 1
fi

echo "✅ Deployment package found: qalam-deployment.zip (11 MB)"
echo ""
echo "📋 Deployment Options:"
echo ""
echo "Option 1 - Manual Upload (Recommended, 2 minutes):"
echo "  1. Copy ZIP to desktop:"
echo "     cp qalam-deployment.zip ~/Desktop/"
echo ""
echo "  2. Go to: https://my.runasp.net/"
echo "     - Login with your credentials"
echo "     - File Manager → Upload qalam-deployment.zip"
echo "     - Extract to site root"
echo ""
echo "Option 2 - Check FTP credentials and use FTP client:"
echo "  1. Get FTP details from RunASP.NET control panel"
echo "  2. Use FileZilla or Cyberduck to upload ./publish/ folder"
echo ""
echo "Option 3 - Install and use lftp for automated FTP:"
echo "     brew install lftp"
echo "     # Then update credentials in deploy.sh and run it"
echo ""
echo "================================================"
echo ""
echo "🎯 Quick Command to copy ZIP to Desktop:"
echo ""
echo "cp qalam-deployment.zip ~/Desktop/"
echo ""
echo "Then upload via browser to: https://my.runasp.net/"
echo ""
echo "After deployment, test at: http://qalam.runasp.net/"
echo ""

# Offer to copy to desktop automatically
read -p "Would you like to copy the ZIP to Desktop now? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    cp qalam-deployment.zip ~/Desktop/
    echo "✅ Copied to ~/Desktop/qalam-deployment.zip"
    echo ""
    echo "Now open: https://my.runasp.net/ and upload the file!"
    open https://my.runasp.net/ 2>/dev/null || echo "Open https://my.runasp.net/ in your browser"
fi

echo ""
echo "✅ Ready for deployment!"
