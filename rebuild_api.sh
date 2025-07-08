#!/bin/bash

echo "🔄 Rebuilding API container with no cache..."

# Stop the API container
echo "📦 Stopping API container..."
docker-compose down api

# Build the API container with no cache
echo "🔨 Building API container with no cache..."
docker-compose build api --no-cache

# Start the API container
echo "🚀 Starting API container..."
docker-compose up -d api

echo "✅ API container rebuild complete!"
echo ""
echo "You can now run the cache test with:"
echo "python python_tests/test_template_cache.py" 