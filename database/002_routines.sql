BEGIN;
SET search_path TO tool_lending, public;
CREATE OR REPLACE FUNCTION tier_checkout_limit(p_tier member_tier) RETURNS integer LANGUAGE sql IMMUTABLE STRICT AS $$ SELECT CASE p_tier WHEN 'STANDARD' THEN 2 WHEN 'SUPPORTER' THEN 5 WHEN 'STAFF' THEN 10 END $$;
CREATE OR REPLACE FUNCTION tier_max_loan_days(p_tier member_tier) RETURNS integer LANGUAGE sql IMMUTABLE STRICT AS $$ SELECT CASE p_tier WHEN 'STANDARD' THEN 7 WHEN 'SUPPORTER' THEN 14 WHEN 'STAFF' THEN 30 END $$;

CREATE OR REPLACE FUNCTION reserve_tool(p_tool_id integer, p_member_id integer, p_starts_on date, p_ends_on date, p_actor varchar, p_request_id uuid)
RETURNS TABLE(reservation_id bigint, status reservation_status) LANGUAGE plpgsql AS $$
DECLARE v_tool tools%ROWTYPE; v_member members%ROWTYPE; v_id bigint;
BEGIN
 IF p_starts_on < CURRENT_DATE OR p_ends_on < p_starts_on THEN RAISE EXCEPTION 'Reservation dates are invalid' USING ERRCODE='TL001'; END IF;
 SELECT * INTO STRICT v_member FROM members WHERE member_id=p_member_id;
 IF NOT v_member.active THEN RAISE EXCEPTION 'Member is inactive' USING ERRCODE='TL002'; END IF;
 SELECT * INTO STRICT v_tool FROM tools WHERE tool_id=p_tool_id FOR UPDATE;
 IF v_tool.status <> 'AVAILABLE' THEN RAISE EXCEPTION 'Tool is not available' USING ERRCODE='TL003'; END IF;
 IF EXISTS(SELECT 1 FROM reservations WHERE tool_id=p_tool_id AND reservations.status='ACTIVE') THEN RAISE EXCEPTION 'Tool already has an active reservation' USING ERRCODE='TL004'; END IF;
 INSERT INTO reservations(tool_id,member_id,starts_on,ends_on) VALUES(p_tool_id,p_member_id,p_starts_on,p_ends_on) RETURNING reservations.reservation_id INTO v_id;
 UPDATE tools SET status='RESERVED',version=version+1 WHERE tool_id=p_tool_id;
 INSERT INTO audit_log(actor,operation,entity_type,entity_id,request_id,details) VALUES(p_actor,'RESERVE','reservation',v_id::text,p_request_id,jsonb_build_object('toolId',p_tool_id,'memberId',p_member_id));
 RETURN QUERY SELECT v_id,'ACTIVE'::reservation_status;
EXCEPTION WHEN NO_DATA_FOUND THEN RAISE EXCEPTION 'Tool or member was not found' USING ERRCODE='TL404';
END $$;

CREATE OR REPLACE FUNCTION checkout_tool(p_tool_id integer,p_member_id integer,p_due_on date,p_actor varchar,p_request_id uuid)
RETURNS TABLE(loan_id bigint,due_on date,status loan_status) LANGUAGE plpgsql AS $$
DECLARE v_tool tools%ROWTYPE; v_member members%ROWTYPE; v_count integer; v_id bigint; v_res_id bigint;
BEGIN
 SELECT * INTO STRICT v_member FROM members WHERE member_id=p_member_id;
 IF NOT v_member.active THEN RAISE EXCEPTION 'Member is inactive' USING ERRCODE='TL002'; END IF;
 IF EXISTS(SELECT 1 FROM loans l WHERE l.member_id=p_member_id AND l.status='OPEN' AND l.due_on<CURRENT_DATE) THEN RAISE EXCEPTION 'Member has an overdue loan' USING ERRCODE='TL005'; END IF;
 SELECT count(*) INTO v_count FROM loans WHERE member_id=p_member_id AND loans.status='OPEN';
 IF v_count>=tier_checkout_limit(v_member.tier) THEN RAISE EXCEPTION 'Member has reached the checkout limit' USING ERRCODE='TL006'; END IF;
 IF p_due_on<CURRENT_DATE OR p_due_on>CURRENT_DATE+tier_max_loan_days(v_member.tier) THEN RAISE EXCEPTION 'Due date exceeds member tier allowance' USING ERRCODE='TL007'; END IF;
 SELECT * INTO STRICT v_tool FROM tools WHERE tool_id=p_tool_id FOR UPDATE;
 IF v_tool.status NOT IN ('AVAILABLE','RESERVED') THEN RAISE EXCEPTION 'Tool is not available for checkout' USING ERRCODE='TL003'; END IF;
 SELECT r.reservation_id INTO v_res_id FROM reservations r WHERE r.tool_id=p_tool_id AND r.status='ACTIVE' FOR UPDATE;
 IF v_res_id IS NOT NULL AND NOT EXISTS(SELECT 1 FROM reservations WHERE reservation_id=v_res_id AND member_id=p_member_id) THEN RAISE EXCEPTION 'Tool is reserved by another member' USING ERRCODE='TL008'; END IF;
 INSERT INTO loans(tool_id,member_id,due_on) VALUES(p_tool_id,p_member_id,p_due_on) RETURNING loans.loan_id INTO v_id;
 IF v_res_id IS NOT NULL THEN UPDATE reservations SET status='FULFILLED' WHERE reservation_id=v_res_id; END IF;
 UPDATE tools SET status='CHECKED_OUT',version=version+1 WHERE tool_id=p_tool_id;
 INSERT INTO audit_log(actor,operation,entity_type,entity_id,request_id,details) VALUES(p_actor,'CHECKOUT','loan',v_id::text,p_request_id,jsonb_build_object('toolId',p_tool_id,'memberId',p_member_id,'dueOn',p_due_on));
 RETURN QUERY SELECT v_id,p_due_on,'OPEN'::loan_status;
EXCEPTION WHEN NO_DATA_FOUND THEN RAISE EXCEPTION 'Tool or member was not found' USING ERRCODE='TL404';
END $$;

CREATE OR REPLACE FUNCTION return_tool(p_loan_id bigint,p_actor varchar,p_request_id uuid)
RETURNS TABLE(loan_id bigint,returned_at timestamptz,late_fee numeric) LANGUAGE plpgsql AS $$
DECLARE v_loan loans%ROWTYPE; v_fee numeric(10,2); v_returned timestamptz:=clock_timestamp();
BEGIN
 SELECT * INTO STRICT v_loan FROM loans WHERE loans.loan_id=p_loan_id FOR UPDATE;
 IF v_loan.status<>'OPEN' THEN RAISE EXCEPTION 'Loan is already returned' USING ERRCODE='TL009'; END IF;
 SELECT GREATEST(0,CURRENT_DATE-v_loan.due_on)*t.daily_late_fee INTO v_fee FROM tools t WHERE t.tool_id=v_loan.tool_id;
 UPDATE loans SET status='RETURNED',returned_at=v_returned,late_fee=v_fee WHERE loans.loan_id=p_loan_id;
 UPDATE tools SET status='AVAILABLE',version=version+1 WHERE tool_id=v_loan.tool_id;
 INSERT INTO audit_log(actor,operation,entity_type,entity_id,request_id,details) VALUES(p_actor,'RETURN','loan',p_loan_id::text,p_request_id,jsonb_build_object('toolId',v_loan.tool_id,'lateFee',v_fee));
 RETURN QUERY SELECT p_loan_id,v_returned,v_fee;
EXCEPTION WHEN NO_DATA_FOUND THEN RAISE EXCEPTION 'Loan was not found' USING ERRCODE='TL404';
END $$;
COMMIT;
