#!/bin/bash

echo "🗑️ REDIS CACHE CLEARING TOOL"
echo "================================"

# Redis connection details
REDIS_HOST=${REDIS_HOST:-"localhost"}
REDIS_PORT=${REDIS_PORT:-"6379"}
REDIS_PASSWORD=${REDIS_PASSWORD:-"redis_password_123"}

echo "🔄 Connecting to Redis at $REDIS_HOST:$REDIS_PORT..."

# Check if redis-cli is available
if ! command -v redis-cli &> /dev/null; then
    echo "❌ redis-cli is not installed or not in PATH"
    echo "💡 Please install Redis CLI or use the Python script instead"
    exit 1
fi

# Test connection
if ! redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" -a "$REDIS_PASSWORD" ping &> /dev/null; then
    echo "❌ Failed to connect to Redis"
    echo "💡 Check your Redis connection details:"
    echo "   REDIS_HOST=$REDIS_HOST"
    echo "   REDIS_PORT=$REDIS_PORT"
    echo "   REDIS_PASSWORD=$REDIS_PASSWORD"
    exit 1
fi

echo "✅ Connected to Redis successfully"

# Get key count
KEY_COUNT=$(redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" -a "$REDIS_PASSWORD" dbsize)
echo "📋 Found $KEY_COUNT cache keys"

if [ "$KEY_COUNT" -eq 0 ]; then
    echo "ℹ️ No cache keys found"
    exit 0
fi

# Show some sample keys
echo "📝 Sample keys:"
redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" -a "$REDIS_PASSWORD" keys "*" | head -10

if [ "$KEY_COUNT" -gt 10 ]; then
    echo "  ... and $((KEY_COUNT - 10)) more keys"
fi

# Confirm deletion
echo ""
echo "⚠️ About to delete $KEY_COUNT cache keys"
read -p "Do you want to continue? (y/N): " -n 1 -r
echo

if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Operation cancelled"
    exit 0
fi

# Clear all keys
echo "🔄 Deleting all cache keys..."
redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" -a "$REDIS_PASSWORD" flushdb

# Verify deletion
REMAINING_KEYS=$(redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" -a "$REDIS_PASSWORD" dbsize)
echo "✅ Successfully deleted cache keys"
echo "📋 Remaining keys: $REMAINING_KEYS"

if [ "$REMAINING_KEYS" -gt 0 ]; then
    echo "⚠️ Some keys remain (might be system keys):"
    redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" -a "$REDIS_PASSWORD" keys "*"
fi

echo ""
echo "✅ Cache clearing completed successfully!" 