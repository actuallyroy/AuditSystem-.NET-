#!/usr/bin/env python3
"""
Test runner for UserService and UsersController comprehensive tests
Installs requirements and runs all test suites with detailed reporting
"""
import subprocess
import sys
import os
import time
import requests
from pathlib import Path
from datetime import datetime

# Try to import psycopg2, make it optional
try:
    import psycopg2
    psycopg2_available = True
except ImportError:
    psycopg2_available = False

def install_requirements():
    """Install required packages"""
    print("📦 Installing test requirements...")
    try:
        subprocess.check_call([
            sys.executable, "-m", "pip", "install", "-r", "requirements.txt"
        ])
        print("✅ Requirements installed successfully")
        return True
    except subprocess.CalledProcessError as e:
        print(f"❌ Failed to install requirements: {e}")
        return False

def check_api_availability():
    """Check if the API is available"""
    print("🔍 Checking API availability...")
    try:
        response = requests.get("http://localhost:8080/health", timeout=5)
        if response.status_code == 200:
            print("✅ API is running and accessible")
            return True
        else:
            print(f"❌ API returned status code: {response.status_code}")
            return False
    except requests.exceptions.RequestException as e:
        print(f"❌ API is not accessible: {e}")
        print("   Make sure your Docker containers are running:")
        print("   docker-compose up -d")
        return False

def check_database_connection():
    """Check if the database is accessible"""
    if not psycopg2_available:
        print("⚠️  Database connection check skipped: psycopg2 not available")
        print("   Database tests will be skipped")
        return True
    
    print("🗄️ Checking database connection...")
    try:
        conn = psycopg2.connect(
            host="localhost",
            port=5432,
            database="retail-execution-audit-system",
            user="postgres",
            password="123456"
        )
        conn.close()
        print("✅ Database connection successful")
        return True
    except Exception as e:
        print(f"❌ Database connection failed: {e}")
        print("   Make sure PostgreSQL container is running")
        return False

def run_pytest_suite(test_file=None, verbose=True, coverage=True):
    """Run pytest test suite"""
    cmd = [sys.executable, "-m", "pytest"]
    
    if test_file:
        cmd.append(test_file)
    
    if verbose:
        cmd.extend(["-v", "--tb=short"])
    
    if coverage:
        cmd.extend(["--cov=.", "--cov-report=html", "--cov-report=term"])
    
    # Add HTML report
    cmd.extend(["--html=test_reports/report.html", "--self-contained-html"])
    
    # Add JUnit XML for CI/CD
    cmd.append("--junitxml=test_reports/junit.xml")
    
    print(f"🧪 Running tests: {' '.join(cmd)}")
    
    # Create reports directory
    os.makedirs("test_reports", exist_ok=True)
    
    try:
        result = subprocess.run(cmd, capture_output=True, text=True)
        
        print("\n" + "="*80)
        print("TEST OUTPUT:")
        print("="*80)
        print(result.stdout)
        
        if result.stderr:
            print("\n" + "="*80)
            print("ERROR OUTPUT:")
            print("="*80)
            print(result.stderr)
        
        return result.returncode == 0
    except subprocess.CalledProcessError as e:
        print(f"❌ Test execution failed: {e}")
        return False

def run_individual_test_suites():
    """Run each test suite individually"""
    test_files = [
        "test_users_controller_api.py",
        "test_authentication_flows.py", 
        "test_user_service_database.py"
    ]
    
    results = {}
    
    for test_file in test_files:
        if os.path.exists(test_file):
            print(f"\n🎯 Running {test_file}...")
            print("-" * 60)
            success = run_pytest_suite(test_file, verbose=True, coverage=False)
            results[test_file] = success
            
            if success:
                print(f"✅ {test_file} - PASSED")
            else:
                print(f"❌ {test_file} - FAILED")
        else:
            print(f"⚠️ {test_file} not found, skipping")
            results[test_file] = None
    
    return results

def run_all_tests():
    """Run all tests together"""
    print("\n🎯 Running all tests together...")
    print("-" * 60)
    return run_pytest_suite(verbose=True, coverage=True)

def generate_summary(individual_results, all_tests_result):
    """Generate test execution summary"""
    print("\n" + "="*80)
    print("TEST EXECUTION SUMMARY")
    print("="*80)
    
    # Individual test results
    print("\nIndividual Test Suites:")
    for test_file, result in individual_results.items():
        if result is None:
            status = "SKIPPED"
            icon = "⚠️"
        elif result:
            status = "PASSED"
            icon = "✅"
        else:
            status = "FAILED"
            icon = "❌"
        
        print(f"  {icon} {test_file:<35} {status}")
    
    # Overall result
    print(f"\nAll Tests Combined:")
    if all_tests_result:
        print("  ✅ All tests combined                    PASSED")
    else:
        print("  ❌ All tests combined                    FAILED")
    
    # Reports location
    print(f"\nTest Reports Generated:")
    print(f"  📊 HTML Report: test_reports/report.html")
    print(f"  📋 Coverage Report: htmlcov/index.html") 
    print(f"  📄 JUnit XML: test_reports/junit.xml")
    
    return all_tests_result

def main():
    """Main test execution function"""
    print("🚀 UserService & UsersController Test Suite")
    print("="*80)
    
    # Change to test directory
    test_dir = Path(__file__).parent
    os.chdir(test_dir)
    
    # Step 1: Install requirements
    if not install_requirements():
        sys.exit(1)
    
    # Step 2: Check prerequisites
    if not check_api_availability():
        sys.exit(1)
    
    if not check_database_connection():
        sys.exit(1)
    
    print("\n✅ All prerequisites met, starting tests...")
    time.sleep(1)
    
    # Step 3: Run individual test suites
    individual_results = run_individual_test_suites()
    
    # Step 4: Run all tests together
    all_tests_result = run_all_tests()
    
    # Step 5: Generate summary
    success = generate_summary(individual_results, all_tests_result)
    
    if success:
        print("\n🎉 All tests completed successfully!")
        sys.exit(0)
    else:
        print("\n💥 Some tests failed. Check the reports for details.")
        sys.exit(1)

if __name__ == "__main__":
    main() 