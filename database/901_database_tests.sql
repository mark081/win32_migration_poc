\set ON_ERROR_STOP on
BEGIN; SET search_path TO tool_lending,public;
DO $$ DECLARE v_state text; BEGIN
 IF tier_checkout_limit('STANDARD')<>2 OR tier_max_loan_days('SUPPORTER')<>14 THEN RAISE EXCEPTION 'tier rules failed'; END IF;
 BEGIN PERFORM * FROM checkout_tool(5,1,CURRENT_DATE+1,'db.test',gen_random_uuid()); RAISE EXCEPTION 'maintenance checkout should fail'; EXCEPTION WHEN SQLSTATE 'TL003' THEN GET STACKED DIAGNOSTICS v_state=RETURNED_SQLSTATE; END;
 IF v_state<>'TL003' THEN RAISE EXCEPTION 'wrong maintenance error'; END IF; v_state:=NULL;
 BEGIN PERFORM * FROM checkout_tool(1,3,CURRENT_DATE+1,'db.test',gen_random_uuid()); RAISE EXCEPTION 'overdue checkout should fail'; EXCEPTION WHEN SQLSTATE 'TL005' THEN GET STACKED DIAGNOSTICS v_state=RETURNED_SQLSTATE; END;
 IF v_state<>'TL005' THEN RAISE EXCEPTION 'wrong overdue error'; END IF;
END $$; ROLLBACK; \echo Database tests passed
