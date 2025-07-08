#!/usr/bin/env python3
"""
Script to clear all Redis cache entries.
This script connects to Redis and clears all cache keys.
"""

import redis
import sys
import os

def clear_all_redis_cache():
    """Clear all Redis cache entries."""
    try:
        # Connect to Redis
        # You can modify these connection details based on your setup
        redis_host = os.getenv('REDIS_HOST', 'localhost')
        redis_port = int(os.getenv('REDIS_PORT', 6379))
        redis_password = os.getenv('REDIS_PASSWORD', 'redis_password_123')
        redis_db = int(os.getenv('REDIS_DB', 0))
        
        print(f"🔄 Connecting to Redis at {redis_host}:{redis_port}...")
        
        # Create Redis connection
        r = redis.Redis(
            host=redis_host,
            port=redis_port,
            password=redis_password,
            db=redis_db,
            decode_responses=True
        )
        
        # Test connection
        r.ping()
        print("✅ Connected to Redis successfully")
        
        # Get all keys
        print("🔄 Getting all cache keys...")
        all_keys = r.keys('*')
        
        if not all_keys:
            print("ℹ️ No cache keys found")
            return True
        
        print(f"📋 Found {len(all_keys)} cache keys")
        
        # Show some sample keys
        print("📝 Sample keys:")
        for i, key in enumerate(all_keys[:10]):
            print(f"  {i+1}. {key}")
        
        if len(all_keys) > 10:
            print(f"  ... and {len(all_keys) - 10} more keys")
        
        # Confirm deletion
        print(f"\n⚠️ About to delete {len(all_keys)} cache keys")
        response = input("Do you want to continue? (y/N): ").strip().lower()
        
        if response not in ['y', 'yes']:
            print("❌ Operation cancelled")
            return False
        
        # Delete all keys
        print("🔄 Deleting all cache keys...")
        deleted_count = r.delete(*all_keys)
        
        print(f"✅ Successfully deleted {deleted_count} cache keys")
        
        # Verify deletion
        remaining_keys = r.keys('*')
        print(f"📋 Remaining keys: {len(remaining_keys)}")
        
        if remaining_keys:
            print("⚠️ Some keys remain (might be system keys):")
            for key in remaining_keys:
                print(f"  - {key}")
        
        return True
        
    except redis.ConnectionError as e:
        print(f"❌ Failed to connect to Redis: {e}")
        print("\n💡 Make sure Redis is running and connection details are correct.")
        print("You can set environment variables:")
        print("  REDIS_HOST=localhost")
        print("  REDIS_PORT=6379")
        print("  REDIS_PASSWORD=your_password")
        print("  REDIS_DB=0")
        return False
        
    except Exception as e:
        print(f"❌ Error clearing Redis cache: {e}")
        return False

def clear_template_cache_only():
    """Clear only template-related cache entries."""
    try:
        # Connect to Redis
        redis_host = os.getenv('REDIS_HOST', 'localhost')
        redis_port = int(os.getenv('REDIS_PORT', 6379))
        redis_password = os.getenv('REDIS_PASSWORD', 'redis_password_123')
        redis_db = int(os.getenv('REDIS_DB', 0))
        
        print(f"🔄 Connecting to Redis at {redis_host}:{redis_port}...")
        
        # Create Redis connection
        r = redis.Redis(
            host=redis_host,
            port=redis_port,
            password=redis_password,
            db=redis_db,
            decode_responses=True
        )
        
        # Test connection
        r.ping()
        print("✅ Connected to Redis successfully")
        
        # Get template keys
        print("🔄 Getting template cache keys...")
        template_keys = r.keys('template:*')
        
        if not template_keys:
            print("ℹ️ No template cache keys found")
            return True
        
        print(f"📋 Found {len(template_keys)} template cache keys")
        
        # Show keys
        print("📝 Template keys:")
        for i, key in enumerate(template_keys):
            print(f"  {i+1}. {key}")
        
        # Delete template keys
        print("🔄 Deleting template cache keys...")
        deleted_count = r.delete(*template_keys)
        
        print(f"✅ Successfully deleted {deleted_count} template cache keys")
        
        return True
        
    except redis.ConnectionError as e:
        print(f"❌ Failed to connect to Redis: {e}")
        return False
        
    except Exception as e:
        print(f"❌ Error clearing template cache: {e}")
        return False

def main():
    """Main function."""
    print("=" * 50)
    print("🗑️ REDIS CACHE CLEARING TOOL")
    print("=" * 50)
    
    if len(sys.argv) > 1 and sys.argv[1] == '--templates-only':
        print("🎯 Clearing template cache only...")
        success = clear_template_cache_only()
    else:
        print("🎯 Clearing all Redis cache...")
        success = clear_all_redis_cache()
    
    if success:
        print("\n✅ Cache clearing completed successfully!")
        sys.exit(0)
    else:
        print("\n❌ Cache clearing failed!")
        sys.exit(1)

if __name__ == "__main__":
    main() 