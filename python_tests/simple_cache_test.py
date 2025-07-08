#!/usr/bin/env python3
"""
Simple cache validation test.
Create a template, fetch all templates, check if cache is working.
"""

import requests
import json
import time
import sys

def simple_cache_test():
    """Simple test to check cache validation."""
    base_url = "http://localhost:8080"
    auth_token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1NTRjZTE5YS02NTAzLTRkOWUtYjI1NS03ZDg1ODgyZGRlMjIiLCJ1bmlxdWVfbmFtZSI6ImFtaXRrdW1hcjkzNTI1QGdtYWlsLmNvbSIsImdpdmVuX25hbWUiOiJBbWl0IiwiZmFtaWx5X25hbWUiOiJLdW1hciIsInJvbGUiOiJtYW5hZ2VyIiwiZW1haWwiOiJhbWl0a3VtYXI5MzUyNUBnbWFpbC5jb20iLCJvcmdhbmlzYXRpb25faWQiOiI4NWU3NDMzNi04M2MwLTQ3MWEtYWM5ZC1lOWQwOWQ3MjU2ZTQiLCJuYmYiOjE3NTE5ODM2MTQsImV4cCI6MTc1MjAxMjQxNCwiaWF0IjoxNzUxOTgzNjE0LCJpc3MiOiJBdWRpdFN5c3RlbSIsImF1ZCI6IkF1ZGl0U3lzdGVtQ2xpZW50cyJ9.LMXkKBwcp8jadle3PqstX7yMB5lWn8A0nmlSYTDR9sc"
    
    headers = {
        "Accept": "*/*",
        "Authorization": f"Bearer {auth_token}",
        "Content-Type": "application/json"
    }
    
    print("🧪 SIMPLE CACHE VALIDATION TEST")
    print("=" * 50)
    
    # Step 1: Get initial templates count
    print("📋 Step 1: Getting initial templates...")
    response = requests.get(f"{base_url}/api/v1/Templates", headers=headers)
    if response.status_code != 200:
        print(f"❌ Failed to get templates: {response.status_code}")
        return False
    
    initial_templates = response.json()
    initial_count = len(initial_templates)
    print(f"✅ Found {initial_count} templates initially")
    
    # Show initial templates
    print(f"📋 Initial templates:")
    for i, template in enumerate(initial_templates):
        print(f"  {i+1}. {template.get('name')} (ID: {template.get('templateId')})")
    
    # Step 2: Create a new template
    print(f"\n📝 Step 2: Creating new template...")
    template_data = {
        "name": f"Cache Test {int(time.time())}",
        "description": "Simple cache test template",
        "category": "Test",
        "questions": {
            "sections": [
                {
                    "title": "Test Section",
                    "questions": [
                        {
                            "id": "test-question",
                            "text": "Test Question",
                            "type": "text",
                            "required": True
                        }
                    ]
                }
            ]
        },
        "scoringRules": {
            "maxScore": 100,
            "passThreshold": 70
        }
    }
    
    response = requests.post(f"{base_url}/api/v1/Templates", headers=headers, json=template_data)
    if response.status_code != 201:
        print(f"❌ Failed to create template: {response.status_code}")
        print(f"Response: {response.text}")
        return False
    
    created_template = response.json()
    template_id = created_template.get('templateId')
    print(f"✅ Template created with ID: {template_id}")
    
    # Check if template exists individually
    print(f"\n🔍 Checking if template exists individually...")
    response = requests.get(f"{base_url}/api/v1/Templates/{template_id}", headers=headers)
    if response.status_code == 200:
        print(f"✅ Template exists individually (not a cache issue)")
    else:
        print(f"❌ Template doesn't exist individually (creation failed)")
        return False
    
    # Step 3: Fetch templates again (should include new template)
    print(f"\n📋 Step 3: Fetching templates after creation...")
    response = requests.get(f"{base_url}/api/v1/Templates", headers=headers)
    if response.status_code != 200:
        print(f"❌ Failed to get templates: {response.status_code}")
        return False
    
    templates_after_creation = response.json()
    after_creation_count = len(templates_after_creation)
    print(f"✅ Found {after_creation_count} templates after creation")
    
    # Check if new template is in the list
    template_in_list = any(t.get('templateId') == template_id for t in templates_after_creation)
    
    # Show what templates were returned
    print(f"\n📋 Templates returned:")
    for i, template in enumerate(templates_after_creation):
        print(f"  {i+1}. {template.get('name')} (ID: {template.get('templateId')})")
    
    if template_in_list:
        print(f"✅ New template found in templates list")
    else:
        print(f"❌ New template NOT found in templates list")
        print("This indicates a CACHE ISSUE - the new template should appear in the list")
    
    # Step 4: Delete the template
    print(f"\n🗑️ Step 4: Deleting template...")
    response = requests.delete(f"{base_url}/api/v1/Templates/{template_id}", headers=headers)
    if response.status_code != 204:
        print(f"❌ Failed to delete template: {response.status_code}")
        return False
    
    print(f"✅ Template deleted successfully")
    
    # Step 5: Fetch templates again (should not include deleted template)
    print(f"\n📋 Step 5: Fetching templates after deletion...")
    response = requests.get(f"{base_url}/api/v1/Templates", headers=headers)
    if response.status_code != 200:
        print(f"❌ Failed to get templates: {response.status_code}")
        return False
    
    templates_after_deletion = response.json()
    after_deletion_count = len(templates_after_deletion)
    print(f"✅ Found {after_deletion_count} templates after deletion")
    
    # Check if deleted template is still in the list
    template_still_in_list = any(t.get('templateId') == template_id for t in templates_after_deletion)
    if template_still_in_list:
        print(f"❌ CACHE ISSUE: Deleted template still appears in templates list!")
        print("This indicates cache invalidation is not working properly")
        return False
    else:
        print(f"✅ Deleted template no longer appears in templates list")
    
    # Summary
    print(f"\n📊 SUMMARY:")
    print(f"  Initial templates: {initial_count}")
    print(f"  After creation: {after_creation_count}")
    print(f"  After deletion: {after_deletion_count}")
    
    if after_creation_count > initial_count and after_deletion_count == initial_count:
        print(f"\n✅ CACHE VALIDATION PASSED!")
        print("Template creation and deletion are working correctly with proper cache invalidation")
        return True
    else:
        print(f"\n❌ CACHE VALIDATION FAILED!")
        print("There might be an issue with cache invalidation or template filtering")
        return False

if __name__ == "__main__":
    try:
        success = simple_cache_test()
        if success:
            print("\n✅ Test completed successfully!")
            sys.exit(0)
        else:
            print("\n❌ Test failed!")
            sys.exit(1)
    except Exception as e:
        print(f"\n❌ Unexpected error: {e}")
        sys.exit(1) 