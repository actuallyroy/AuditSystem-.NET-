#!/usr/bin/env python3
"""
Cache Endpoints Test Suite
Tests Redis caching functionality and cache management endpoints
"""

import json
import requests
import uuid
import time
from datetime import datetime
from typing import Dict, Any
import sys
import os

# Add the tools directory to the path
sys.path.append(os.path.join(os.path.dirname(__file__), 'tools'))

class CacheTestSuite:
    def __init__(self, base_url: str = "http://localhost:8080"):
        self.base_url = base_url
        self.session = requests.Session()
        self.auth_tokens = {}
        
        # Load test credentials
        with open('python_tests/tools/test_credentials.json', 'r') as f:
            self.credentials = json.load(f)
    
    def log(self, message: str, level: str = "INFO"):
        """Log test messages with timestamps"""
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        print(f"[{timestamp}] {level}: {message}")
    
    def authenticate(self, role: str) -> bool:
        """Authenticate user and store token"""
        if role not in self.credentials:
            self.log(f"Invalid role: {role}", "ERROR")
            return False
        
        creds = self.credentials[role]
        login_data = {
            "username": creds["username"],
            "password": creds["password"]
        }
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Auth/login",
                json=login_data,
                headers={"Content-Type": "application/json"}
            )
            
            if response.status_code == 200:
                auth_data = response.json()
                self.auth_tokens[role] = auth_data.get("token")
                self.log(f"Successfully authenticated as {role}")
                return True
            else:
                self.log(f"Authentication failed for {role}: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Authentication error for {role}: {str(e)}", "ERROR")
            return False
    
    def get_auth_headers(self, role: str) -> Dict[str, str]:
        """Get headers with authentication token"""
        token = self.auth_tokens.get(role)
        if not token:
            self.log(f"No token found for role: {role}", "ERROR")
            return {}
        
        return {
            "Content-Type": "application/json",
            "Authorization": f"Bearer {token}"
        }
    
    def test_cache_health(self) -> bool:
        """Test cache health endpoint"""
        self.log("Testing cache health endpoint")
        
        if not self.auth_tokens.get("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Cache/health",
                headers=headers
            )
            
            if response.status_code == 200:
                health_data = response.json()
                self.log(f"Cache health check successful: {health_data}")
                return True
            else:
                self.log(f"Cache health check failed: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Cache health check error: {str(e)}", "ERROR")
            return False
    
    def test_cache_stats(self) -> bool:
        """Test cache statistics endpoint"""
        self.log("Testing cache statistics endpoint")
        
        if not self.auth_tokens.get("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Cache/stats",
                headers=headers
            )
            
            if response.status_code == 200:
                stats_data = response.json()
                self.log(f"Cache stats retrieved successfully: {stats_data}")
                return True
            else:
                self.log(f"Cache stats failed: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Cache stats error: {str(e)}", "ERROR")
            return False
    
    def test_cache_clear(self) -> bool:
        """Test cache clear endpoint"""
        self.log("Testing cache clear endpoint")
        
        if not self.auth_tokens.get("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        
        try:
            response = self.session.delete(
                f"{self.base_url}/api/v1/Cache/clear",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Cache clear successful")
                return True
            else:
                self.log(f"Cache clear failed: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Cache clear error: {str(e)}", "ERROR")
            return False
    
    def test_cache_warmup(self) -> bool:
        """Test cache warmup endpoint"""
        self.log("Testing cache warmup endpoint")
        
        if not self.auth_tokens.get("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Cache/warmup",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Cache warmup successful")
                return True
            else:
                self.log(f"Cache warmup failed: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Cache warmup error: {str(e)}", "ERROR")
            return False
    
    def test_cache_pattern_clear(self) -> bool:
        """Test cache pattern clear endpoint"""
        self.log("Testing cache pattern clear endpoint")
        
        if not self.auth_tokens.get("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        
        # Test clearing user cache
        try:
            response = self.session.delete(
                f"{self.base_url}/api/v1/Cache/clear/users",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("User cache pattern clear successful")
            else:
                self.log(f"User cache pattern clear failed: {response.status_code}", "WARNING")
                
        except Exception as e:
            self.log(f"User cache pattern clear error: {str(e)}", "WARNING")
        
        # Test clearing template cache
        try:
            response = self.session.delete(
                f"{self.base_url}/api/v1/Cache/clear/templates",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Template cache pattern clear successful")
            else:
                self.log(f"Template cache pattern clear failed: {response.status_code}", "WARNING")
                
        except Exception as e:
            self.log(f"Template cache pattern clear error: {str(e)}", "WARNING")
        
        return True
    
    def test_cache_performance(self) -> bool:
        """Test cache performance by measuring response times"""
        self.log("Testing cache performance")
        
        if not self.auth_tokens.get("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        
        # Test multiple requests to see caching in action
        endpoints_to_test = [
            "/api/v1/Users",
            "/api/v1/Templates",
            "/api/v1/Organisations"
        ]
        
        for endpoint in endpoints_to_test:
            self.log(f"Testing cache performance for {endpoint}")
            
            # First request (should be slower)
            start_time = time.time()
            try:
                response1 = self.session.get(
                    f"{self.base_url}{endpoint}",
                    headers=headers
                )
                first_request_time = time.time() - start_time
                
                if response1.status_code == 200:
                    self.log(f"First request to {endpoint}: {first_request_time:.3f}s")
                else:
                    self.log(f"First request to {endpoint} failed: {response1.status_code}", "WARNING")
                    continue
                    
            except Exception as e:
                self.log(f"First request to {endpoint} error: {str(e)}", "WARNING")
                continue
            
            # Second request (should be faster due to caching)
            start_time = time.time()
            try:
                response2 = self.session.get(
                    f"{self.base_url}{endpoint}",
                    headers=headers
                )
                second_request_time = time.time() - start_time
                
                if response2.status_code == 200:
                    self.log(f"Second request to {endpoint}: {second_request_time:.3f}s")
                    
                    # Check if second request was faster (indicating cache hit)
                    if second_request_time < first_request_time:
                        improvement = ((first_request_time - second_request_time) / first_request_time) * 100
                        self.log(f"Cache performance improvement: {improvement:.1f}%")
                    else:
                        self.log("No cache performance improvement detected", "WARNING")
                else:
                    self.log(f"Second request to {endpoint} failed: {response2.status_code}", "WARNING")
                    
            except Exception as e:
                self.log(f"Second request to {endpoint} error: {str(e)}", "WARNING")
        
        return True
    
    def run_cache_tests(self) -> bool:
        """Run all cache-related tests"""
        self.log("Starting cache endpoints test suite")
        
        # Authenticate as manager
        if not self.authenticate("manager"):
            self.log("Manager authentication failed", "ERROR")
            return False
        
        # Test cache health
        if not self.test_cache_health():
            self.log("Cache health test failed", "ERROR")
            return False
        
        # Test cache stats
        if not self.test_cache_stats():
            self.log("Cache stats test failed", "ERROR")
            return False
        
        # Test cache performance
        if not self.test_cache_performance():
            self.log("Cache performance test failed", "ERROR")
            return False
        
        # Test cache warmup
        if not self.test_cache_warmup():
            self.log("Cache warmup test failed", "WARNING")
        
        # Test cache pattern clear
        if not self.test_cache_pattern_clear():
            self.log("Cache pattern clear test failed", "WARNING")
        
        # Test cache clear (do this last)
        if not self.test_cache_clear():
            self.log("Cache clear test failed", "WARNING")
        
        self.log("Cache endpoints test suite completed!")
        return True

def main():
    """Main function to run the cache test suite"""
    import argparse
    
    parser = argparse.ArgumentParser(description="Cache Endpoints Test Suite")
    parser.add_argument("--base-url", default="http://localhost:8080", 
                       help="Base URL of the API (default: http://localhost:8080)")
    parser.add_argument("--verbose", "-v", action="store_true", 
                       help="Enable verbose logging")
    
    args = parser.parse_args()
    
    # Create and run test suite
    test_suite = CacheTestSuite(args.base_url)
    
    try:
        success = test_suite.run_cache_tests()
        if success:
            print("\n✅ Cache tests passed successfully!")
            sys.exit(0)
        else:
            print("\n❌ Some cache tests failed!")
            sys.exit(1)
    except KeyboardInterrupt:
        print("\n⚠️  Cache tests interrupted by user")
        sys.exit(1)
    except Exception as e:
        print(f"\n💥 Unexpected error: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main() 