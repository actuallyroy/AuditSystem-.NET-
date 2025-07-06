#!/usr/bin/env python3
"""
Quick API Test Script
A simple script to quickly verify basic API functionality
"""

import json
import requests
import sys
from datetime import datetime

def log(message, level="INFO"):
    """Simple logging function"""
    timestamp = datetime.now().strftime("%H:%M:%S")
    print(f"[{timestamp}] {level}: {message}")

def test_health_check(base_url):
    """Test basic health check"""
    log("Testing health check...")
    try:
        response = requests.get(f"{base_url}/health", timeout=10)
        if response.status_code == 200:
            log("✅ Health check passed")
            return True
        else:
            log(f"❌ Health check failed: {response.status_code}", "ERROR")
            return False
    except Exception as e:
        log(f"❌ Health check error: {str(e)}", "ERROR")
        return False

def test_authentication(base_url):
    """Test basic authentication"""
    log("Testing authentication...")
    
    # Load credentials
    try:
        with open('python_tests/tools/test_credentials.json', 'r') as f:
            credentials = json.load(f)
    except Exception as e:
        log(f"❌ Failed to load credentials: {str(e)}", "ERROR")
        return False
    
    # Test manager login
    try:
        creds = credentials["manager"]
        login_data = {
            "username": creds["username"],
            "password": creds["password"]
        }
        
        response = requests.post(
            f"{base_url}/api/v1/Auth/login",
            json=login_data,
            headers={"Content-Type": "application/json"},
            timeout=10
        )
        
        if response.status_code == 200:
            auth_data = response.json()
            token = auth_data.get("token")
            if token:
                log("✅ Manager authentication successful")
                return token
            else:
                log("❌ No token in response", "ERROR")
                return False
        else:
            log(f"❌ Manager authentication failed: {response.status_code}", "ERROR")
            return False
            
    except Exception as e:
        log(f"❌ Authentication error: {str(e)}", "ERROR")
        return False

def test_basic_endpoints(base_url, token):
    """Test basic endpoints"""
    log("Testing basic endpoints...")
    
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {token}"
    }
    
    endpoints = [
        "/api/v1/Users",
        "/api/v1/Organisations", 
        "/api/v1/Templates",
        "/api/v1/Assignments",
        "/api/v1/Audits"
    ]
    
    success_count = 0
    total_count = len(endpoints)
    
    for endpoint in endpoints:
        try:
            response = requests.get(
                f"{base_url}{endpoint}",
                headers=headers,
                timeout=10
            )
            
            if response.status_code == 200:
                log(f"✅ {endpoint} - OK")
                success_count += 1
            else:
                log(f"❌ {endpoint} - {response.status_code}", "ERROR")
                
        except Exception as e:
            log(f"❌ {endpoint} - Error: {str(e)}", "ERROR")
    
    log(f"Basic endpoints: {success_count}/{total_count} passed")
    return success_count == total_count

def test_cache_endpoints(base_url, token):
    """Test cache endpoints"""
    log("Testing cache endpoints...")
    
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {token}"
    }
    
    cache_endpoints = [
        "/api/v1/Cache/health",
        "/api/v1/Cache/stats"
    ]
    
    success_count = 0
    total_count = len(cache_endpoints)
    
    for endpoint in cache_endpoints:
        try:
            response = requests.get(
                f"{base_url}{endpoint}",
                headers=headers,
                timeout=10
            )
            
            if response.status_code == 200:
                log(f"✅ {endpoint} - OK")
                success_count += 1
            else:
                log(f"❌ {endpoint} - {response.status_code}", "WARNING")
                
        except Exception as e:
            log(f"❌ {endpoint} - Error: {str(e)}", "WARNING")
    
    log(f"Cache endpoints: {success_count}/{total_count} passed")
    return success_count > 0  # At least one should work

def main():
    """Main function"""
    import argparse
    
    parser = argparse.ArgumentParser(description="Quick API Test")
    parser.add_argument("--base-url", default="http://localhost:8080", 
                       help="Base URL of the API (default: http://localhost:8080)")
    
    args = parser.parse_args()
    
    log("Starting quick API test...")
    log(f"Testing API at: {args.base_url}")
    
    # Test health check
    if not test_health_check(args.base_url):
        log("❌ API health check failed. Is the server running?", "ERROR")
        sys.exit(1)
    
    # Test authentication
    token = test_authentication(args.base_url)
    if not token:
        log("❌ Authentication failed. Check credentials.", "ERROR")
        sys.exit(1)
    
    # Test basic endpoints
    basic_ok = test_basic_endpoints(args.base_url, token)
    
    # Test cache endpoints
    cache_ok = test_cache_endpoints(args.base_url, token)
    
    # Summary
    log("=" * 50)
    log("QUICK TEST SUMMARY")
    log("=" * 50)
    
    if basic_ok and cache_ok:
        log("🎉 All tests passed! API is working correctly.")
        log("✅ Health check: OK")
        log("✅ Authentication: OK") 
        log("✅ Basic endpoints: OK")
        log("✅ Cache endpoints: OK")
        sys.exit(0)
    elif basic_ok:
        log("⚠️  Most tests passed, but cache may have issues.")
        log("✅ Health check: OK")
        log("✅ Authentication: OK")
        log("✅ Basic endpoints: OK")
        log("⚠️  Cache endpoints: Some issues")
        sys.exit(0)
    else:
        log("❌ Some tests failed. Check the API configuration.")
        log("✅ Health check: OK")
        log("✅ Authentication: OK")
        log("❌ Basic endpoints: Issues detected")
        log("⚠️  Cache endpoints: Issues detected")
        sys.exit(1)

if __name__ == "__main__":
    main() 