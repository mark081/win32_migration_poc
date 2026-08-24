\set ON_ERROR_STOP on

SELECT format('CREATE ROLE tool_lending_app LOGIN PASSWORD %L', :'app_password')
WHERE NOT EXISTS (
    SELECT 1 FROM pg_roles WHERE rolname = 'tool_lending_app'
) \gexec

SELECT 'CREATE DATABASE tool_lending OWNER tool_lending_app'
WHERE NOT EXISTS (
    SELECT 1 FROM pg_database WHERE datname = 'tool_lending'
) \gexec

ALTER ROLE tool_lending_app PASSWORD :'app_password';
