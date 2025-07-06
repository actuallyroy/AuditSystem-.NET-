#!/usr/bin/env python3

import subprocess
import sys
import os
import time

def run_command(command, cwd=None):
    """Run a command and return the result."""
    print(f"[CMD] Running: {command}")
    try:
        result = subprocess.run(
            command, 
            shell=True, 
            cwd=cwd,
            capture_output=True, 
            text=True
        )
        if result.returncode == 0:
            print(f"[OK] Command succeeded")
            if result.stdout:
                print(f"[OUTPUT] {result.stdout}")
            return True
        else:
            print(f"[ERROR] Command failed with return code {result.returncode}")
            if result.stderr:
                print(f"[ERROR] {result.stderr}")
            return False
    except Exception as e:
        print(f"[ERROR] Exception running command: {e}")
        return False

def rebuild_application():
    """Rebuild the .NET application using Docker."""
    print("\n🔨 Rebuilding the application using Docker...")
    
    # Stop any existing containers
    print("🛑 Stopping existing containers...")
    run_command("docker-compose down")
    
    # Build the application
    print("🔨 Building Docker images...")
    if not run_command("docker-compose build --no-cache"):
        print("❌ Failed to build Docker images")
        return False
    
    return True

def start_application():
    """Start the application using Docker Compose."""
    print("\n🚀 Starting the application using Docker Compose...")
    
    # Start all services
    print("🚀 Starting all services...")
    if not run_command("docker-compose up -d"):
        print("❌ Failed to start services")
        return False
    
    # Wait for services to be ready
    print("⏳ Waiting for services to be ready...")
    time.sleep(30)  # Wait for all services to start
    
    # Check if the API is responding
    max_retries = 12  # 2 minutes total
    retry_count = 0
    
    while retry_count < max_retries:
        try:
            import requests
            response = requests.get("http://localhost:8080/health", timeout=5)
            if response.status_code == 200:
                print("✅ API is ready and responding")
                return True
        except:
            pass
        
        retry_count += 1
        print(f"⏳ Waiting for API to be ready... ({retry_count}/{max_retries})")
        time.sleep(10)
    
    # If health check fails, try to check if the service is at least running
    print("⚠️  Health check failed, checking if service is running...")
    result = run_command("docker-compose ps")
    if result:
        print("ℹ️  Services are running, proceeding with tests...")
        return True
    
    print("❌ Services are not running properly")
    return False

def run_tests():
    """Run the Python tests."""
    print("\n🧪 Running tests...")
    
    # Install required packages
    print("📦 Installing required packages...")
    if not run_command("pip install requests pytest"):
        print("❌ Failed to install packages")
        return False
    
    # Run the simple test
    test_file = os.path.join("python_tests", "test_organisation_simple.py")
    if os.path.exists(test_file):
        print(f"🧪 Running test: {test_file}")
        if not run_command(f"python {test_file}"):
            print("❌ Simple test failed")
            return False
    else:
        print(f"❌ Test file not found: {test_file}")
        return False
    
    return True

def cleanup_application():
    """Clean up Docker containers and resources."""
    print("\n🧹 Cleaning up Docker resources...")
    
    # Stop and remove containers
    if not run_command("docker-compose down"):
        print("⚠️  Failed to stop containers gracefully")
    
    # Remove unused images and volumes (optional)
    print("🧹 Removing unused Docker resources...")
    run_command("docker system prune -f")
    
    print("✅ Cleanup completed")

def main():
    """Main function to orchestrate the rebuild and test process."""
    print("🚀 Starting Audit System Rebuild and Test Process (Docker)")
    print("=" * 60)
    
    try:
        # Step 1: Rebuild the application
        if not rebuild_application():
            print("❌ Failed to rebuild application")
            sys.exit(1)
        
        # Step 2: Start the application
        if not start_application():
            print("❌ Failed to start application")
            cleanup_application()
            sys.exit(1)
        
        # Step 3: Run tests
        if not run_tests():
            print("❌ Tests failed")
            cleanup_application()
            sys.exit(1)
        
        print("\n🎉 All steps completed successfully!")
        print("=" * 60)
        print("✅ Application rebuilt with Docker")
        print("✅ Application started with Docker Compose")
        print("✅ Tests passed")
        
        # Ask user if they want to keep the services running
        print("\n❓ Do you want to keep the Docker services running? (y/N)")
        try:
            response = input().lower().strip()
            if response in ['y', 'yes']:
                print("✅ Docker services are still running")
                print("🔗 API: http://localhost:8080")
                print("🔗 pgAdmin: http://localhost:5050")
                print("🔗 RabbitMQ Management: http://localhost:15672")
                print("💡 Use 'docker-compose down' to stop all services")
            else:
                cleanup_application()
        except KeyboardInterrupt:
            print("\n🛑 Interrupted by user")
            cleanup_application()
        
        return True
        
    except Exception as e:
        print(f"\n❌ Unexpected error: {e}")
        cleanup_application()
        sys.exit(1)

if __name__ == "__main__":
    main() 