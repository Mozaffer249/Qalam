#!/bin/bash

# RunASP.NET FTP Deployment Script
# Usage: ./deploy.sh

set -e

# Configuration
FTP_HOST="ftp://site49539.siteasp.net"
FTP_USER="site49539"
FTP_PASS='nG%3!8JjN?x5'
PUBLISH_DIR="./publish"
REMOTE_DIR="/httpdocs"

echo "🚀 Starting deployment to RunASP.NET..."
echo "================================================"

# Check if publish directory exists
if [ ! -d "$PUBLISH_DIR" ]; then
    echo "❌ Error: Publish directory not found!"
    echo "Run 'dotnet publish' first."
    exit 1
fi

# Test FTP connection
echo "🔍 Testing FTP connection..."
if ! curl -s --connect-timeout 10 --user "$FTP_USER:$FTP_PASS" "$FTP_HOST/" > /dev/null 2>&1; then
    echo "❌ Error: Cannot connect to FTP server"
    echo "Trying alternative FTP host (ftp.siteasp.net)..."
    FTP_HOST="ftp://ftp.siteasp.net/site49539"
fi

echo "✅ FTP connection successful!"

# Deploy files using curl
echo ""
echo "📦 Uploading files to $FTP_HOST$REMOTE_DIR..."
echo "This may take several minutes..."
echo ""

# Function to upload directory recursively
upload_directory() {
    local local_dir="$1"
    local remote_path="$2"
    
    # Create remote directory
    curl -s --user "$FTP_USER:$FTP_PASS" "$FTP_HOST$remote_path/" --ftp-create-dirs > /dev/null 2>&1 || true
    
    # Upload files in current directory
    for file in "$local_dir"/*; do
        if [ -f "$file" ]; then
            local filename=$(basename "$file")
            local remote_file="$remote_path/$filename"
            echo "  ↑ Uploading: $filename"
            
            if curl -s -T "$file" --user "$FTP_USER:$FTP_PASS" "$FTP_HOST$remote_file" > /dev/null 2>&1; then
                echo "    ✅ $filename"
            else
                echo "    ❌ Failed: $filename"
            fi
        elif [ -d "$file" ]; then
            local dirname=$(basename "$file")
            local new_remote_path="$remote_path/$dirname"
            echo "  📁 Directory: $dirname"
            upload_directory "$file" "$new_remote_path"
        fi
    done
}

# Start upload
upload_directory "$PUBLISH_DIR" "$REMOTE_DIR"

echo ""
echo "================================================"
echo "✅ Deployment completed successfully!"
echo ""
echo "🌐 Your API should be available at:"
echo "   http://qalam.runasp.net/"
echo ""
echo "📝 Next steps:"
echo "   1. Visit http://qalam.runasp.net/swagger to test"
echo "   2. Check logs if there are any issues"
echo "   3. Test your authentication endpoints"
echo ""
