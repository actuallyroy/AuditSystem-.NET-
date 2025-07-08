#!/usr/bin/env python3
"""
Quick cache validation test.
Create a template and immediately check if it appears in the list.
"""

import requests
import json
import time

def quick_cache_test():
    """Quick test to check cache validation."""
    base_url = "http://localhost:8080"
    auth_token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1NTRjZTE5YS02NTAzLTRkOWUtYjI1NS03ZDg1ODgyZGRlMjIiLCJ1bmlxdWVfbmFtZSI6ImFtaXRrdW1hcjkzNTI1QGdtYWlsLmNvbSIsImdpdmVuX25hbWUiOiJBbWl0IiwiZmFtaWx5X25hbWUiOiJLdW1hciIsInJvbGUiOiJtYW5hZ2VyIiwiZW1haWwiOiJhbWl0a3VtYXI5MzUyNUBnbWFpbC5jb20iLCJvcmdhbmlzYXRpb25faWQiOiI4NWU3NDMzNi04M2MwLTQ3MWEtYWM5ZC1lOWQwOWQ3MjU2ZTQiLCJuYmYiOjE3NTE5ODMwMjgsImV4cCI6MTc1MjAxMTgyOCwiaWF0IjoxNzUxOTgzMDI4LCJpc3MiOiJBdWRpdFN5c3RlbSIsImF1ZCI6IkF1ZGl0U3lzdGVtQ2xpZW50cyJ9.NYjWWYZwFWgDzSNMhU1fhgkAB69orfqXVJh--xH9_rs"
    
    headers = {
        "Accept": "*/*",
        "Authorization": f"Bearer {auth_token}",
        "Content-Type": "application/json"
    }
    
    print("🧪 QUICK CACHE VALIDATION TEST")
    print("=" * 40)
    
    # Step 1: Get initial templates
    print("📋 Step 1: Getting initial templates...")
    response = requests.get(f"{base_url}/api/v1/Templates", headers=headers)
    initial_templates = response.json()
    initial_count = len(initial_templates)
    print(f"✅ Found {initial_count} templates initially")
    
    # Step 2: Create a new template
    print(f"\n📝 Step 2: Creating new template...")
    template_data = {
        "name": f"Quick Test {int(time.time())}",
        "description": "Quick cache test template",
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
    template_name = created_template.get('name')
    print(f"✅ Template created: {template_name} (ID: {template_id})")
    
    # Step 3: Immediately fetch templates
    print(f"\n📋 Step 3: Fetching templates immediately after creation...")
    response = requests.get(f"{base_url}/api/v1/Templates", headers=headers)
    templates_after_creation = response.json()
    after_creation_count = len(templates_after_creation)
    print(f"✅ Found {after_creation_count} templates after creation")
    
    # Check if new template is in the list
    template_in_list = any(t.get('templateId') == template_id for t in templates_after_creation)
    
    print(f"\n📋 Templates after creation:")
    for i, template in enumerate(templates_after_creation):
        print(f"  {i+1}. {template.get('name')} (ID: {template.get('templateId')})")
    
    if template_in_list:
        print(f"\n✅ SUCCESS: New template found in templates list!")
        print("Cache invalidation is working correctly.")
        return True
    else:
        print(f"\n❌ CACHE ISSUE: New template NOT found in templates list!")
        print("This indicates cache invalidation is not working properly.")
        return False

if __name__ == "__main__":
    try:
        success = quick_cache_test()
        if success:
            print("\n✅ Test completed successfully!")
        else:
            print("\n❌ Test failed!")
    except Exception as e:
        print(f"\n❌ Unexpected error: {e}") 