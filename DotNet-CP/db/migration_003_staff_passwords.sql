-- ============================================================
--  Migration: real BCrypt password hashes for the seeded staff
--  Reason: seed_sample_data.sql inserts placeholder strings
--  ('$2a$10$placeholderhash1', ...) which are not valid BCrypt hashes, so
--  no one could actually log in once Day 3's JWT auth went live.
--
--  Safe to re-run. Only touches password_hash on the six seeded usernames;
--  any staff you've added yourself through Table Maintenance is untouched
--  (those already get a real generated temp password from StaffService).
--
--  DEV CREDENTIALS (change before anything resembling production):
--    admin   / Admin@123     -- Admin
--    priya   / Priya@123     -- Counselor
--    snehal  / Snehal@123    -- Counselor
--    ravi    / Ravi@123      -- Faculty
--    ananya  / Ananya@123    -- Faculty
--    vikram  / Vikram@123    -- Faculty
-- ============================================================

USE computerseekho;

UPDATE staff SET password_hash = '$2b$10$QmtsipSWiBbWXL4bGyK.a.hkH0CJmsdZKsiwPR/nQ9OdRZD6e/BDi' WHERE username = 'admin';
UPDATE staff SET password_hash = '$2b$10$0W1/0qOmAdIrVmu1ObERXO4vueK.M6/5Yd/As5wW2btKjxTNm636u' WHERE username = 'priya';
UPDATE staff SET password_hash = '$2b$10$zI2cBsOK6nDKFRouzVqNW.tpnj82JJ69QZMZDd9OB8fiSBgjqLSJ.' WHERE username = 'snehal';
UPDATE staff SET password_hash = '$2b$10$GSfenQABOiOHRhAsT4cafeNIrXoTuWRONvTaBn81rCC0Op1f/8lKG' WHERE username = 'ravi';
UPDATE staff SET password_hash = '$2b$10$g4T3cLfrGqJWTpCd/2SL0OmRC585z2NVAixLHXUvW9vL.55J5z73W' WHERE username = 'ananya';
UPDATE staff SET password_hash = '$2b$10$nlkHgf2zljJ.UnvKFELTs.q1I67fmtioBRxsaynjB7VZoKpbwWZ5q' WHERE username = 'vikram';

SELECT username, role, LEFT(password_hash, 7) AS hash_prefix, is_active FROM staff ORDER BY staff_id;
