BEGIN;
SET search_path TO tool_lending, public;

INSERT INTO members(member_id, display_name, tier, active)
VALUES
    (1, 'Standard Sam', 'STANDARD', true),
    (2, 'Supporter Sue', 'SUPPORTER', true),
    (3, 'Overdue Owen', 'STANDARD', true),
    (4, 'Inactive Iris', 'STANDARD', false);

INSERT INTO tools(tool_id, asset_tag, display_name, daily_late_fee, status)
VALUES
    (1, 'TL-001', 'Cordless Drill', 2, 'AVAILABLE'),
    (2, 'TL-002', 'Circular Saw', 3.5, 'RESERVED'),
    (3, 'TL-003', 'Extension Ladder', 5, 'CHECKED_OUT'),
    (4, 'TL-004', 'Tile Cutter', 4, 'CHECKED_OUT'),
    (5, 'TL-005', 'Pressure Washer', 6, 'MAINTENANCE'),
    (6, 'TL-006', 'Socket Set', 1, 'AVAILABLE');

INSERT INTO reservations(reservation_id, tool_id, member_id, starts_on, ends_on, status)
VALUES (1, 2, 2, CURRENT_DATE, CURRENT_DATE + 3, 'ACTIVE');

INSERT INTO loans(loan_id, tool_id, member_id, checked_out_at, due_on, status)
VALUES
    (1, 3, 3, clock_timestamp() - interval '12 days', CURRENT_DATE - 5, 'OPEN'),
    (2, 4, 2, clock_timestamp() - interval '16 days', CURRENT_DATE - 2, 'OPEN');

SELECT setval(pg_get_serial_sequence('members', 'member_id'), (SELECT max(member_id) FROM members));
SELECT setval(pg_get_serial_sequence('tools', 'tool_id'), (SELECT max(tool_id) FROM tools));
SELECT setval(pg_get_serial_sequence('reservations', 'reservation_id'), (SELECT max(reservation_id) FROM reservations));
SELECT setval(pg_get_serial_sequence('loans', 'loan_id'), (SELECT max(loan_id) FROM loans));

COMMIT;
