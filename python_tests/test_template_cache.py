#!/usr/bin/env python3
"""
Test script to verify template cache invalidation after deletion.
This script will:
1. Create a template
2. Delete the template
3. Try to fetch the template to see if it's still cached
"""

import requests
import json
import time
import sys
from typing import Optional

class TemplateCacheTester:
    def __init__(self, base_url: str = "http://localhost:8080"):
        self.base_url = base_url
        self.auth_token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1NTRjZTE5YS02NTAzLTRkOWUtYjI1NS03ZDg1ODgyZGRlMjIiLCJ1bmlxdWVfbmFtZSI6ImFtaXRrdW1hcjkzNTI1QGdtYWlsLmNvbSIsImdpdmVuX25hbWUiOiJBbWl0IiwiZmFtaWx5X25hbWUiOiJLdW1hciIsInJvbGUiOiJtYW5hZ2VyIiwiZW1haWwiOiJhbWl0a3VtYXI5MzUyNUBnbWFpbC5jb20iLCJvcmdhbmlzYXRpb25faWQiOiI4NWU3NDMzNi04M2MwLTQ3MWEtYWM5ZC1lOWQwOWQ3MjU2ZTQiLCJuYmYiOjE3NTE5NTM3OTcsImV4cCI6MTc1MTk4MjU5NywiaWF0IjoxNzUxOTUzNzk3LCJpc3MiOiJBdWRpdFN5c3RlbSIsImF1ZCI6IkF1ZGl0U3lzdGVtQ2xpZW50cyJ9.usf9_GOVRjZlRms0H_uUrZSpJJN8B5uj2Jo8bXP5bmU"
        self.headers = {
            "Accept": "*/*",
            "Accept-Language": "en-US,en;q=0.9",
            "Authorization": f"Bearer {self.auth_token}",
            "Connection": "keep-alive",
            "Content-Type": "application/json",
            "Origin": "http://localhost:3000",
            "Referer": "http://localhost:3000/",
            "Sec-Fetch-Dest": "empty",
            "Sec-Fetch-Mode": "cors",
            "Sec-Fetch-Site": "same-site",
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36",
            "sec-ch-ua": '"Not)A;Brand";v="8", "Chromium";v="138", "Google Chrome";v="138"',
            "sec-ch-ua-mobile": "?0",
            "sec-ch-ua-platform": '"Windows"'
        }
        self.created_template_id = None

    def create_template(self) -> Optional[str]:
        """Create a test template and return the template ID."""
        print("🔄 Creating template...")
        
        template_data = {
            "name": f"Cache Test Template {int(time.time())}",
            "description": "Template for testing cache invalidation",
            "category": "Store Visit",
            "questions": {
                "sections": [
                    {
                        "title": "Store Information",
                        "description": "Basic store details and identification",
                        "questions": [
                            {
                                "id": "question-1751979901355-0.852548596771834",
                                "text": "Store Name",
                                "type": "text",
                                "required": True
                            },
                            {
                                "id": "question-1751979901355-0.7342129764231172",
                                "text": "Store Address",
                                "type": "text",
                                "required": True
                            }
                        ],
                        "conditionalLogic": [],
                        "isVisible": True
                    }
                ]
            },
            "scoringRules": {
                "maxScore": 100,
                "passThreshold": 70,
                "questionScores": {
                    "question-1751979901355-0.852548596771834": 50,
                    "question-1751979901355-0.7342129764231172": 50
                }
            },
            "validFrom": "2025-07-08T13:05:08.970Z",
            "validTo": "2026-07-08T13:05:08.970Z"
        }

        try:
            response = requests.post(
                f"{self.base_url}/api/v1/Templates",
                headers=self.headers,
                json=template_data
            )
            
            if response.status_code == 201:
                template = response.json()
                template_id = template.get('templateId')
                print(f"✅ Template created successfully with ID: {template_id}")
                return template_id
            else:
                print(f"❌ Failed to create template. Status: {response.status_code}")
                print(f"Response: {response.text}")
                return None
                
        except Exception as e:
            print(f"❌ Error creating template: {e}")
            return None

    def delete_template(self, template_id: str) -> bool:
        """Delete the specified template."""
        print(f"🔄 Deleting template {template_id}...")
        
        try:
            response = requests.delete(
                f"{self.base_url}/api/v1/Templates/{template_id}",
                headers=self.headers
            )
            
            if response.status_code == 204:
                print(f"✅ Template {template_id} deleted successfully")
                return True
            else:
                print(f"❌ Failed to delete template. Status: {response.status_code}")
                print(f"Response: {response.text}")
                return False
                
        except Exception as e:
            print(f"❌ Error deleting template: {e}")
            return False

    def fetch_template(self, template_id: str) -> bool:
        """Try to fetch the specified template."""
        print(f"🔄 Fetching template {template_id}...")
        
        try:
            response = requests.get(
                f"{self.base_url}/api/v1/Templates/{template_id}",
                headers=self.headers
            )
            
            if response.status_code == 200:
                template = response.json()
                print(f"❌ Template {template_id} is still accessible (CACHE ISSUE!)")
                print(f"Template data: {json.dumps(template, indent=2)}")
                return True
            elif response.status_code == 404:
                print(f"✅ Template {template_id} not found (correct behavior)")
                return False
            else:
                print(f"⚠️ Unexpected status code: {response.status_code}")
                print(f"Response: {response.text}")
                return False
                
        except Exception as e:
            print(f"❌ Error fetching template: {e}")
            return False

    def fetch_all_templates(self) -> list:
        """Fetch all templates to see if the deleted template appears in the list."""
        print("🔄 Fetching all templates...")
        
        try:
            response = requests.get(
                f"{self.base_url}/api/v1/Templates",
                headers=self.headers
            )
            
            if response.status_code == 200:
                templates = response.json()
                print(f"✅ Found {len(templates)} templates")
                return templates
            else:
                print(f"❌ Failed to fetch templates. Status: {response.status_code}")
                print(f"Response: {response.text}")
                return []
                
        except Exception as e:
            print(f"❌ Error fetching templates: {e}")
            return []

    def clear_template_cache(self, template_id: str) -> bool:
        """Clear cache for a specific template."""
        print(f"🔄 Clearing cache for template {template_id}...")
        
        try:
            response = requests.delete(
                f"{self.base_url}/api/v1/Cache/clear-template/{template_id}",
                headers=self.headers
            )
            
            if response.status_code == 200:
                print(f"✅ Cache cleared for template {template_id}")
                return True
            else:
                print(f"❌ Failed to clear cache. Status: {response.status_code}")
                print(f"Response: {response.text}")
                return False
                
        except Exception as e:
            print(f"❌ Error clearing cache: {e}")
            return False

    def clear_all_template_cache(self) -> bool:
        """Clear all template cache entries."""
        print("🔄 Clearing all template cache...")
        
        try:
            response = requests.delete(
                f"{self.base_url}/api/v1/Cache/clear-all-templates",
                headers=self.headers
            )
            
            if response.status_code == 200:
                print("✅ All template cache cleared")
                return True
            else:
                print(f"❌ Failed to clear all template cache. Status: {response.status_code}")
                print(f"Response: {response.text}")
                return False
                
        except Exception as e:
            print(f"❌ Error clearing all template cache: {e}")
            return False

    def run_test(self):
        """Run the complete cache invalidation test."""
        print("=" * 60)
        print("🧪 TEMPLATE CACHE INVALIDATION TEST")
        print("=" * 60)
        
        # Step 1: Create template
        template_id = self.create_template()
        if not template_id:
            print("❌ Test failed: Could not create template")
            return False
        
        self.created_template_id = template_id
        
        # Step 2: Verify template exists in all templates list
        print("\n" + "=" * 40)
        print("Step 2: Verifying template exists in all templates list")
        print("=" * 40)
        
        all_templates_before = self.fetch_all_templates()
        template_in_list_before = any(t.get('templateId') == template_id for t in all_templates_before)
        
        # Show what templates were returned
        print(f"📋 Templates returned: {len(all_templates_before)}")
        for i, template in enumerate(all_templates_before):
            print(f"  {i+1}. {template.get('name')} (ID: {template.get('templateId')})")
        
        if template_in_list_before:
            print(f"✅ Template {template_id} found in templates list after creation")
        else:
            print(f"❌ Template {template_id} not found in templates list after creation")
            print("This might indicate an issue with the templates list endpoint or filtering")
            
            # Let's also try to fetch the individual template to see if it exists
            print(f"\n🔄 Checking if template {template_id} exists individually...")
            if self.fetch_template(template_id):
                print(f"✅ Template {template_id} exists individually but not in list")
                print("This suggests the templates list endpoint has different filtering logic")
            else:
                print(f"❌ Template {template_id} doesn't exist individually either")
                print("This suggests the template creation failed or was rolled back")
                return False
        
        # Step 3: Delete template
        print("\n" + "=" * 40)
        print("Step 3: Deleting template")
        print("=" * 40)
        
        if not self.delete_template(template_id):
            print("❌ Test failed: Could not delete template")
            return False
        
        # Step 4: Wait a moment for cache operations
        print("\n⏳ Waiting 2 seconds for cache operations...")
        time.sleep(2)
        
        # Step 5: Check if template still appears in all templates list (MAIN TEST)
        print("\n" + "=" * 40)
        print("Step 5: Testing cache invalidation in all templates list")
        print("=" * 40)
        
        all_templates_after = self.fetch_all_templates()
        template_in_list_after = any(t.get('templateId') == template_id for t in all_templates_after)
        
        if template_in_list_after:
            print(f"❌ CACHE ISSUE DETECTED!")
            print(f"Deleted template {template_id} still appears in templates list")
            print("This indicates that cache invalidation is not working properly.")
            
            # Show the templates that were returned
            print(f"\n📋 Templates returned: {len(all_templates_after)}")
            for i, template in enumerate(all_templates_after):
                print(f"  {i+1}. {template.get('name')} (ID: {template.get('templateId')})")
            
            # Try to clear cache manually
            print(f"\n🔄 Attempting manual cache clearing...")
            self.clear_template_cache(template_id)
            time.sleep(1)
            
            # Test again
            print(f"\n🔄 Testing again after manual cache clear...")
            all_templates_after_clear = self.fetch_all_templates()
            template_in_list_after_clear = any(t.get('templateId') == template_id for t in all_templates_after_clear)
            
            if template_in_list_after_clear:
                print(f"❌ Template still appears in list after manual cache clear")
                
                # Try clearing all template cache
                print(f"\n🔄 Attempting to clear all template cache...")
                self.clear_all_template_cache()
                time.sleep(1)
                
                # Test one more time
                print(f"\n🔄 Final test after clearing all template cache...")
                all_templates_final = self.fetch_all_templates()
                template_in_list_final = any(t.get('templateId') == template_id for t in all_templates_final)
                
                if template_in_list_final:
                    print(f"❌ Template still appears in list after clearing all template cache")
                    print("This suggests a deeper caching issue.")
                else:
                    print(f"✅ Template no longer appears in list after clearing all template cache")
            else:
                print(f"✅ Template no longer appears in list after manual cache clear")
            
            return False
        else:
            print(f"\n✅ CACHE INVALIDATION WORKING!")
            print(f"Deleted template {template_id} no longer appears in templates list")
            print("Cache invalidation is working properly.")
        
        # Step 6: Also test individual template fetch for completeness
        print("\n" + "=" * 40)
        print("Step 6: Testing individual template fetch")
        print("=" * 40)
        
        template_still_accessible = self.fetch_template(template_id)
        
        if template_still_accessible:
            print(f"❌ Individual template fetch still returns deleted template")
            return False
        else:
            print(f"✅ Individual template fetch correctly returns 404")
        
        print("\n" + "=" * 60)
        print("🎉 ALL TESTS PASSED! Cache invalidation is working correctly.")
        print("=" * 60)
        return True

def main():
    """Main function to run the test."""
    tester = TemplateCacheTester()
    
    try:
        success = tester.run_test()
        if success:
            print("\n✅ Test completed successfully!")
            sys.exit(0)
        else:
            print("\n❌ Test failed!")
            sys.exit(1)
    except KeyboardInterrupt:
        print("\n⚠️ Test interrupted by user")
        sys.exit(1)
    except Exception as e:
        print(f"\n❌ Unexpected error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main() 