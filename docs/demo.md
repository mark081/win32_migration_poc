# Demonstration walkthrough

Use API key `demo-local-key` and actor `demo.operator`.

1. Refresh inventory. Observe available, reserved, checked-out, overdue, and maintenance tools.
2. Load member 1 (Standard Sam) and check out tool 1 for seven days.
3. Repeat the same request with the same idempotency key; the API returns the original result.
4. Attempt to check out maintenance tool 5; PostgreSQL rejects it even if client validation is bypassed.
5. Load member 3 (Overdue Owen) and attempt a checkout; both native precheck and database authority reject it.
6. Return seeded loan 2 and observe the database-calculated late fee.
7. Open the audit view and correlate each successful write with actor and request ID.

The PowerShell integration suite automates these scenarios and includes two concurrent requests for the same tool.
