#!/usr/bin/env python3
"""
Test credentials module for loading authentication credentials
"""

import json
import os

def get_test_credentials(role="manager"):
    """
    Load test credentials from JSON file
    
    Args:
        role (str): Role to get credentials for (manager, auditor)
        
    Returns:
        dict: Credentials for the specified role
    """
    try:
        # Get the directory where this script is located
        script_dir = os.path.dirname(os.path.abspath(__file__))
        credentials_file = os.path.join(script_dir, "test_credentials.json")
        
        with open(credentials_file, 'r') as f:
            credentials = json.load(f)
        
        if role in credentials:
            return credentials[role]
        else:
            # Fallback to manager if role not found
            return credentials.get("manager", {})
            
    except Exception as e:
        print(f"Error loading credentials: {e}")
        # Return default credentials
        return {
            "username": "amitkumar93525@gmail.com",
            "password": "idDcXT.as5tAK2g"
        }

def get_all_credentials():
    """
    Load all test credentials from JSON file
    
    Returns:
        dict: All credentials
    """
    try:
        # Get the directory where this script is located
        script_dir = os.path.dirname(os.path.abspath(__file__))
        credentials_file = os.path.join(script_dir, "test_credentials.json")
        
        with open(credentials_file, 'r') as f:
            return json.load(f)
            
    except Exception as e:
        print(f"Error loading credentials: {e}")
        return {} 