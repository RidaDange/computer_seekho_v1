-- ============================================================
--  Day 3 sample data: open enquiries + their pending follow-ups
--
--  seed_sample_data.sql truncates `followups` and never repopulates it, so
--  the Follow-up landing page would come up empty on a fresh database even
--  though inquiry #4 is sitting in 'In-Followup'. This gives the screen a
--  realistic starting state: one overdue call and one due today.
--
--  Safe to re-run — it clears only the rows it owns before re-inserting.
-- ============================================================

USE computerseekho;

DELETE FROM followups WHERE followup_id IN (1, 2, 3);
DELETE FROM inquiries WHERE inquiry_id = 6;

-- A fresh, unworked enquiry assigned to Priya (Counselor, staff_id 1).
INSERT INTO inquiries
  (inquiry_id, course_id, staff_id, enquirer_name, email, phone, message, source, status, inquiry_date, closure_reason_id)
VALUES
  (6, 3, 1, 'Meera Kulkarni', 'meera.kulkarni@example.com', '9765432101',
   'Interested in the weekend batch for Java Full Stack.', 'Website', 'New', CURRENT_DATE - INTERVAL 3 DAY, NULL);

-- Pending follow-ups. Enquiry #5 is closed ('Not Interested') and
-- deliberately gets none — a closed enquiry must never surface on the
-- follow-up list.
INSERT INTO followups (followup_id, inquiry_id, staff_id, followup_date, notes, next_followup, status) VALUES
  -- Overdue: enquiry #4 came in on 2026-07-20, first follow-up due +3 days.
  (1, 4, 2, DATE('2026-07-23'), 'Auto-scheduled first follow-up (enquiry date + 3 days).', NULL, 'Pending'),
  -- Due today: enquiry #6's first follow-up, three days after it arrived.
  (2, 6, 1, CURRENT_DATE, 'Auto-scheduled first follow-up (enquiry date + 3 days).', NULL, 'Pending'),
  -- A completed attempt, so the enquiry history isn't blank.
  (3, 4, 2, DATE('2026-07-21'), 'Called once, no answer. Trying again.', DATE('2026-07-23'), 'No Response');

SELECT f.followup_id, i.enquirer_name, i.status AS inquiry_status, f.followup_date, f.status
FROM followups f JOIN inquiries i ON i.inquiry_id = f.inquiry_id
ORDER BY f.followup_id;
