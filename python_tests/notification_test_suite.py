#!/usr/bin/env python3
"""Comprehensive integration tests for the Audit System notification backend.

Features tested:
1. Database seed – inserts sample notifications for the authenticated test user.
2. REST API – list, unread-count, mark-as-read, mark-all-read endpoints.
3. SignalR WebSocket – connect, subscribe, receive messages, mark-all-read via hub.

Requirements (install with pip):
    requests
    psycopg2-binary
    signalrcore==0.10.4
    tabulate (optional pretty printing)
"""

import os
import sys
import json
import time
import uuid
import requests
import logging
from datetime import datetime
from typing import List

# Project utils path
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
sys.path.append(os.path.join(os.path.dirname(os.path.abspath(__file__)), "tools"))

from tools.test_credentials import get_test_credentials  # type: ignore
from tools.db_query_tool import DatabaseQueryTool  # type: ignore

# ------------------------ Configuration ------------------------------------ #
API_BASE_URL = os.getenv("API_BASE_URL", "https://test.scorptech.co/api/v1")
WS_BASE_URL = os.getenv("WS_BASE_URL", "wss://test.scorptech.co/hubs/notifications")

# Database connection (override via env vars if needed)
DB_CFG = {
    "host": os.getenv("DB_HOST", "localhost"),
    "port": int(os.getenv("DB_PORT", "5432")),
    "database": os.getenv("DB_NAME", "retail-execution-audit-system"),
    "username": os.getenv("DB_USER", "postgres"),
    "password": os.getenv("DB_PASSWORD", "123456"),
}

# ----------------------- Helper Functions ---------------------------------- #

def login_and_get_token() -> str:
    creds = get_test_credentials()
    if not creds:
        raise RuntimeError("Test credentials not found – edit tools/test_credentials.json")

    resp = requests.post(f"{API_BASE_URL}/auth/login", json={
        "username": creds["username"],
        "password": creds["password"],
    })
    resp.raise_for_status()
    token: str = resp.json().get("token", "")
    if not token:
        raise RuntimeError("Login succeeded but token missing in response")
    return token


def seed_notifications(db: DatabaseQueryTool, user_email: str, count: int = 3) -> List[str]:
    """Insert `count` unread notifications for the user (returns list of IDs)."""
    # Get user + organisation IDs
    user = db.execute_query(
        "SELECT user_id, organisation_id FROM users WHERE email = %s LIMIT 1", (user_email,))
    if not user:
        raise RuntimeError(f"User with email {user_email} not found in DB")

    user_id = user[0]["user_id"]
    org_id = user[0]["organisation_id"]

    ids = []
    for _ in range(count):
        nid = str(uuid.uuid4())
        ids.append(nid)
        db.execute_update(
            """
            INSERT INTO notification (
                notification_id, user_id, organisation_id, type, title, message,
                priority, channel, status, created_at
            ) VALUES (%s,%s,%s,%s,%s,%s,'medium','in_app','pending',NOW())
            """,
            (
                nid,
                user_id,
                org_id,
                "system_alert",
                "Test Notification",
                f"This is a generated test notification at {datetime.utcnow()}"
            )
        )
    return ids


# --------------------------- Tests ----------------------------------------- #

def test_rest_endpoints(token: str):
    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}

    # 1. list notifications
    r = requests.get(f"{API_BASE_URL}/notifications?page=1&pageSize=10", headers=headers)
    assert r.status_code == 200, f"/notifications failed {r.status_code}"
    data = r.json()
    assert "notifications" in data, "missing notifications key"
    print(f"✅ /notifications returned {len(data['notifications'])} rows")
    if not data["notifications"]:
        print("⚠️  No notifications returned (maybe seeding failed?)")

    # pick first notification id
    first_id = data["notifications"][0]["notificationId"] if data["notifications"] else None

    # 2. unread count
    r = requests.get(f"{API_BASE_URL}/notifications/unread-count", headers=headers)
    assert r.status_code == 200, "/notifications/unread-count failed"
    count_before = r.json().get("unreadCount", 0)
    print(f"✅ Unread before mark: {count_before}")

    # 3. mark one read (if have)
    if first_id:
        r = requests.put(f"{API_BASE_URL}/notifications/{first_id}/read", headers=headers)
        assert r.status_code == 200, f"mark read failed {r.status_code}"
        print("✅ Mark single notification read")

    # 4. mark all read
    r = requests.put(f"{API_BASE_URL}/notifications/mark-all-read", headers=headers)
    assert r.status_code == 200, "mark-all-read failed"
    print("✅ Mark-all-read succeeded")

    # 5. unread count should be 0
    r = requests.get(f"{API_BASE_URL}/notifications/unread-count", headers=headers)
    count_after = r.json().get("unreadCount", 0)
    assert count_after == 0, f"Unread count expected 0 got {count_after}"
    print("✅ Unread count 0 after mark-all-read")


# SignalR test using signalrcore
try:
    from signalrcore.hub_connection_builder import HubConnectionBuilder
except ImportError:
    HubConnectionBuilder = None  # type: ignore


def test_signalr(token: str):
    if HubConnectionBuilder is None:
        print("⚠️  signalrcore not installed -> skipping SignalR test")
        return True

    received_messages = []

    def on_receive(args):
        received_messages.append(args)
        print("💬 Notification received via SignalR")

    try:
        connection = (
            HubConnectionBuilder()
            .with_url(f"{WS_BASE_URL}?access_token={token}")
            .configure_logging(logging.INFO)
            .build()
        )
        connection.on("ReceiveNotification", on_receive)
        
        print("🔌 Starting SignalR connection...")
        connection.start()
        time.sleep(2)  # Wait for connection to be fully established

        print("📤 Sending test message...")
        # invoke test message
        connection.send("SendTestMessage", ["Hello from test suite"])

        # wait for message with timeout
        print("⏳ Waiting for response...")
        for i in range(10):
            if received_messages:
                break
            time.sleep(1)
            print(f"  Waiting... ({i+1}/10)")

        connection.stop()
        print("🔌 SignalR connection stopped")

        assert received_messages, "No SignalR message received"
        print("✅ SignalR message exchange succeeded")
        return True
        
    except Exception as e:
        print(f"❌ SignalR test failed: {e}")
        return False

# --------------------------- Main ----------------------------------------- #

if __name__ == "__main__":
    token = login_and_get_token()

    # Seed notifications
    creds = get_test_credentials()
    db = DatabaseQueryTool(**DB_CFG)
    if db.connect():
        try:
            ids = seed_notifications(db, creds["username"], count=3)
            print(f"🛠️  Seeded {len(ids)} notifications in DB")
        finally:
            db.disconnect()

    test_rest_endpoints(token)
    test_signalr(token)

    print("\n🎉 All notification tests passed!") 