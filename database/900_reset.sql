BEGIN; SET search_path TO tool_lending,public; TRUNCATE audit_log,idempotency_records,loans,reservations,tools,members RESTART IDENTITY CASCADE; COMMIT;
\ir 003_seed.sql
