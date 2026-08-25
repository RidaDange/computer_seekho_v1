-- ============================================================
--  Day 4 sample data: joinable batches, enrolments, payments, drives
--
--  Why this is needed:
--   1. Both seeded batches are 'Completed', so the registration wizard had
--      no batch to offer and step 3 was a dead end.
--   2. The seeded students have no enrollments or payments rows, so the
--      Students screen showed no fee position and no receipts.
--   3. placement_drives was never seeded at all.
--
--  Receipt numbers follow FeeCalculator.receiptNumberFor():
--      VITA/<financial year>/<5-digit student id>-<installment>
--  Indian FY runs April-March, so a July 2024 registration is 2024-25.
--
--  Safe to re-run — clears the rows it owns first.
-- ============================================================

USE computerseekho;

SET FOREIGN_KEY_CHECKS = 0;
DELETE FROM payments;
DELETE FROM enrollments;
DELETE FROM placement_drives;
DELETE FROM batches WHERE batch_id IN (3, 4, 5);
SET FOREIGN_KEY_CHECKS = 1;

-- ------------------------------------------------------------
-- Batches you can actually register into (one per course)
-- ------------------------------------------------------------
INSERT INTO batches
  (batch_id, course_id, staff_id, batch_name, academic_year, start_date, end_date, timing, capacity, current_count, status, is_active)
VALUES
  (3, 1, 3, 'PG-DAC-Aug-2026',  '2026-27', '2026-08-03', '2027-01-29', '9 AM - 5 PM',  30, 0, 'Upcoming', 1),
  (4, 2, 4, 'PG-DBDA-Aug-2026', '2026-27', '2026-08-03', '2027-01-29', '9 AM - 5 PM',  25, 0, 'Upcoming', 1),
  (5, 3, 5, 'JFS-Weekend-2026', '2026-27', '2026-08-08', '2026-11-07', 'Sat-Sun 10-4', 20, 0, 'Ongoing',  1);

-- ------------------------------------------------------------
-- Enrolments for the already-registered students
--   students 1 & 3 -> PG-DAC batch 1, student 2 -> PG-DBDA batch 2
-- ------------------------------------------------------------
INSERT INTO enrollments (enrollment_id, student_id, batch_id, inquiry_id, enroll_date, status) VALUES
  (1, 1, 1, 1, '2024-07-10', 'Completed'),
  (2, 2, 2, 2, '2024-07-12', 'Completed'),
  (3, 3, 1, 3, '2024-07-14', 'Completed');

-- ------------------------------------------------------------
-- Payments. PG-DAC is 90,000 (45,000 x 2); PG-DBDA is 95,000 (47,500 x 2).
-- Student 3 has only paid installment 1 on purpose, so the Students screen
-- has an outstanding balance to demonstrate the Collect flow against.
-- ------------------------------------------------------------
INSERT INTO payments
  (payment_id, student_id, enrollment_id, amount, installment_number, total_installments, payment_date, payment_mode, payment_status, transaction_id, receipt_no, remarks)
VALUES
  (1, 1, 1, 45000.00, 1, 2, '2024-07-10', 'UPI',           'Success', 'UPI2407101122', 'VITA/2024-25/00001-1', 'Registration installment'),
  (2, 1, 1, 45000.00, 2, 2, '2024-08-09', 'Bank Transfer', 'Success', 'NEFT240809551', 'VITA/2024-25/00001-2', 'Final installment'),
  (3, 2, 2, 47500.00, 1, 2, '2024-07-12', 'Cash',          'Success', NULL,            'VITA/2024-25/00002-1', 'Registration installment'),
  (4, 2, 2, 47500.00, 2, 2, '2024-08-11', 'UPI',           'Success', 'UPI2408110987', 'VITA/2024-25/00002-2', 'Final installment'),
  (5, 3, 3, 45000.00, 1, 2, '2024-07-14', 'Card',          'Success', 'CARD240714778', 'VITA/2024-25/00003-1', 'Registration installment');

-- ------------------------------------------------------------
-- Placement drives (the visits behind the existing placement_records)
-- ------------------------------------------------------------
INSERT INTO placement_drives
  (drive_id, recruiter_id, course_id, drive_date, drive_mode, position, description, eligibility_criteria, package, hr_contact_name, hr_contact_email, hr_contact_phone, no_of_openings, no_of_students_selected, drive_status)
VALUES
  (1, 1, 1, '2025-02-05', 'Offline', 'Software Engineer',
   'Campus drive for the PG-DAC Aug 2024 batch.', 'PG-DAC completed, no active backlogs',
   750000.00, 'Anita Deshmukh', 'anita.deshmukh@example.com', '9820100001', 5, 1, 'Completed'),
  (2, 2, 2, '2025-02-08', 'Hybrid', 'Data Analyst',
   'Analytics hiring for the PG-DBDA batch.', 'PG-DBDA completed',
   700000.00, 'Rakesh Menon', 'rakesh.menon@example.com', '9820100002', 3, 1, 'Completed'),
  (3, 4, NULL, '2026-08-20', 'Online', 'Associate Software Engineer',
   'Open drive across all completed batches.', 'Any completed PG diploma',
   680000.00, 'Farah Sheikh', 'farah.sheikh@example.com', '9820100003', 8, NULL, 'Scheduled');

-- Point the existing placement records at the drives that produced them.
UPDATE placement_records SET drive_id = 1 WHERE student_id = 1;
UPDATE placement_records SET drive_id = 2 WHERE student_id = 2;
UPDATE placement_records SET drive_id = 1 WHERE student_id = 3;

-- Resync headcounts with the enrolments just inserted. Counts everything
-- except Dropped, matching EnrollmentRepository.countByBatch_BatchIdAndStatusNot:
-- a student who completed the course was still in the batch.
UPDATE batches b
SET current_count = (
  SELECT COUNT(*) FROM enrollments e WHERE e.batch_id = b.batch_id AND e.status <> 'Dropped'
);

SELECT b.batch_id, b.batch_name, b.status, b.current_count, b.capacity FROM batches b ORDER BY b.batch_id;
SELECT s.student_id, CONCAT(s.first_name,' ',s.last_name) AS student,
       COUNT(p.payment_id) AS payments, IFNULL(SUM(p.amount),0) AS paid
FROM students s LEFT JOIN payments p ON p.student_id = s.student_id
GROUP BY s.student_id ORDER BY s.student_id;
