#!/usr/bin/env python3
"""
Master Test Runner
Executes all test suites in the correct order and provides comprehensive reporting
"""

import sys
import os
import time
import json
from datetime import datetime
from typing import Dict, List, Any
import subprocess
import argparse

class TestRunner:
    def __init__(self, base_url: str = "http://localhost:8080"):
        self.base_url = base_url
        self.results = {}
        self.start_time = None
        self.end_time = None
        
    def log(self, message: str, level: str = "INFO"):
        """Log messages with timestamps"""
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        print(f"[{timestamp}] {level}: {message}")
    
    def run_test_suite(self, test_file: str, description: str) -> Dict[str, Any]:
        """Run a specific test suite and return results"""
        self.log(f"Running {description}...")
        
        start_time = time.time()
        
        try:
            # Run the test file as a subprocess
            result = subprocess.run([
                sys.executable, test_file, 
                "--base-url", self.base_url
            ], capture_output=True, text=True, timeout=300)  # 5 minute timeout
            
            end_time = time.time()
            duration = end_time - start_time
            
            success = result.returncode == 0
            
            return {
                "test_file": test_file,
                "description": description,
                "success": success,
                "duration": duration,
                "return_code": result.returncode,
                "stdout": result.stdout,
                "stderr": result.stderr
            }
            
        except subprocess.TimeoutExpired:
            self.log(f"Test suite {description} timed out after 5 minutes", "ERROR")
            return {
                "test_file": test_file,
                "description": description,
                "success": False,
                "duration": 300,
                "return_code": -1,
                "stdout": "",
                "stderr": "Test suite timed out"
            }
        except Exception as e:
            self.log(f"Error running {description}: {str(e)}", "ERROR")
            return {
                "test_file": test_file,
                "description": description,
                "success": False,
                "duration": 0,
                "return_code": -1,
                "stdout": "",
                "stderr": str(e)
            }
    
    def run_all_tests(self) -> bool:
        """Run all test suites in the correct order"""
        self.log("Starting comprehensive API test suite")
        self.start_time = time.time()
        
        # Define test suites in execution order
        test_suites = [
            ("test_api_comprehensive.py", "Comprehensive API Tests"),
            ("test_cache_endpoints.py", "Cache Endpoints Tests"),
            ("test_error_handling.py", "Error Handling Tests"),
            ("test_missing_endpoints.py", "Missing Endpoints Tests")
        ]
        
        # Check if test files exist
        missing_files = []
        for test_file, description in test_suites:
            if not os.path.exists(test_file):
                missing_files.append(test_file)
        
        if missing_files:
            self.log(f"Missing test files: {missing_files}", "ERROR")
            return False
        
        # Run each test suite
        for test_file, description in test_suites:
            result = self.run_test_suite(test_file, description)
            self.results[description] = result
            
            if result["success"]:
                self.log(f"✅ {description} completed successfully in {result['duration']:.2f}s")
            else:
                self.log(f"❌ {description} failed in {result['duration']:.2f}s", "ERROR")
                if result["stderr"]:
                    self.log(f"Error details: {result['stderr']}", "ERROR")
        
        self.end_time = time.time()
        return self.generate_report()
    
    def generate_report(self) -> bool:
        """Generate comprehensive test report"""
        self.log("Generating test report...")
        
        total_duration = self.end_time - self.start_time
        total_tests = len(self.results)
        passed_tests = sum(1 for result in self.results.values() if result["success"])
        failed_tests = total_tests - passed_tests
        
        # Print summary
        print("\n" + "="*80)
        print("COMPREHENSIVE API TEST SUITE REPORT")
        print("="*80)
        print(f"Test Run: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print(f"Base URL: {self.base_url}")
        print(f"Total Duration: {total_duration:.2f} seconds")
        print(f"Total Test Suites: {total_tests}")
        print(f"Passed: {passed_tests}")
        print(f"Failed: {failed_tests}")
        print(f"Success Rate: {(passed_tests/total_tests)*100:.1f}%" if total_tests > 0 else "N/A")
        
        # Print detailed results
        print("\n" + "-"*80)
        print("DETAILED RESULTS")
        print("-"*80)
        
        for description, result in self.results.items():
            status = "✅ PASS" if result["success"] else "❌ FAIL"
            print(f"{status} | {description} | {result['duration']:.2f}s")
            
            if not result["success"] and result["stderr"]:
                print(f"    Error: {result['stderr'][:200]}...")
        
        # Print recommendations
        print("\n" + "-"*80)
        print("RECOMMENDATIONS")
        print("-"*80)
        
        if failed_tests == 0:
            print("🎉 All tests passed! The API is working correctly.")
            print("✅ Authentication and authorization are functioning properly")
            print("✅ All CRUD operations are working as expected")
            print("✅ Cache functionality is operational")
            print("✅ Error handling is working correctly")
        else:
            print("⚠️  Some tests failed. Please review the following:")
            
            for description, result in self.results.items():
                if not result["success"]:
                    print(f"   • {description}: Check the error details above")
            
            print("\nCommon issues to check:")
            print("   • Ensure the API server is running")
            print("   • Verify database connectivity")
            print("   • Check Redis connection for cache tests")
            print("   • Review authentication credentials")
            print("   • Check API endpoint configurations")
        
        # Save detailed report to file
        report_data = {
            "timestamp": datetime.now().isoformat(),
            "base_url": self.base_url,
            "total_duration": total_duration,
            "summary": {
                "total_tests": total_tests,
                "passed": passed_tests,
                "failed": failed_tests,
                "success_rate": (passed_tests/total_tests)*100 if total_tests > 0 else 0
            },
            "results": self.results
        }
        
        report_filename = f"test_report_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
        try:
            with open(report_filename, 'w') as f:
                json.dump(report_data, f, indent=2)
            self.log(f"Detailed report saved to: {report_filename}")
        except Exception as e:
            self.log(f"Failed to save report: {str(e)}", "WARNING")
        
        print("\n" + "="*80)
        
        return failed_tests == 0
    
    def run_specific_test(self, test_name: str) -> bool:
        """Run a specific test suite"""
        test_mapping = {
            "comprehensive": ("test_api_comprehensive.py", "Comprehensive API Tests"),
            "cache": ("test_cache_endpoints.py", "Cache Endpoints Tests"),
            "errors": ("test_error_handling.py", "Error Handling Tests"),
            "missing": ("test_missing_endpoints.py", "Missing Endpoints Tests")
        }
        
        if test_name not in test_mapping:
            self.log(f"Unknown test suite: {test_name}", "ERROR")
            self.log(f"Available test suites: {list(test_mapping.keys())}")
            return False
        
        test_file, description = test_mapping[test_name]
        
        if not os.path.exists(test_file):
            self.log(f"Test file not found: {test_file}", "ERROR")
            return False
        
        result = self.run_test_suite(test_file, description)
        self.results[description] = result
        
        if result["success"]:
            self.log(f"✅ {description} completed successfully")
            return True
        else:
            self.log(f"❌ {description} failed", "ERROR")
            return False

def main():
    """Main function"""
    parser = argparse.ArgumentParser(description="Master Test Runner for Retail Audit API")
    parser.add_argument("--base-url", default="http://localhost:8080", 
                       help="Base URL of the API (default: http://localhost:8080)")
    parser.add_argument("--test", choices=["comprehensive", "cache", "errors", "missing"],
                       help="Run a specific test suite")
    parser.add_argument("--verbose", "-v", action="store_true", 
                       help="Enable verbose logging")
    
    args = parser.parse_args()
    
    # Create test runner
    runner = TestRunner(args.base_url)
    
    try:
        if args.test:
            # Run specific test
            success = runner.run_specific_test(args.test)
        else:
            # Run all tests
            success = runner.run_all_tests()
        
        if success:
            print("\n🎉 All requested tests passed!")
            sys.exit(0)
        else:
            print("\n❌ Some tests failed!")
            sys.exit(1)
            
    except KeyboardInterrupt:
        print("\n⚠️  Tests interrupted by user")
        sys.exit(1)
    except Exception as e:
        print(f"\n💥 Unexpected error: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main() 