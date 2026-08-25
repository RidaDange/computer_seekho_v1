-- ============================================================
--  ComputerSeekho — Comprehensive dummy data
--  Populates every table the public site and admin screens read from.
--  WARNING: this TRUNCATEs the tables listed below first, so any rows
--  you've manually added via Table Maintenance testing will be replaced.
--  Nothing outside this list is touched.
-- ============================================================

USE computerseekho;

SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE placement_records;
TRUNCATE TABLE payments;
TRUNCATE TABLE enrollments;
TRUNCATE TABLE followups;
TRUNCATE TABLE students;
TRUNCATE TABLE inquiries;
TRUNCATE TABLE batches;
TRUNCATE TABLE course_staff;
TRUNCATE TABLE courses;
TRUNCATE TABLE course_categories;
TRUNCATE TABLE staff;
TRUNCATE TABLE recruiters;
TRUNCATE TABLE closure_reasons;
TRUNCATE TABLE announcements;
TRUNCATE TABLE banners;
TRUNCATE TABLE testimonials;
TRUNCATE TABLE news_events;
TRUNCATE TABLE gallery_images;
TRUNCATE TABLE contact_messages;

SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- Course categories
-- ============================================================
INSERT INTO course_categories (category_id, name, age_group, description, is_active) VALUES
  (1, 'PG Diploma', 'Graduate', 'Post-graduate diploma programs authorized by C-DAC ACTS', 1),
  (2, 'Certification', 'All', 'Short-term industry certification courses', 1);

-- ============================================================
-- Staff (2 counselors, 3 faculty, 1 admin)
-- ============================================================
INSERT INTO staff (staff_id, name, email, phone, role, qualification, experience, photo_url, username, password_hash, is_active) VALUES
  (1, 'Priya Sharma', 'priya.sharma@vita.com', '9820011111', 'Counselor', 'MBA', 4.5, 'https://i.pravatar.cc/300?img=47', 'priya', '$2a$10$placeholderhash1', 1),
  (2, 'Snehal Joshi', 'snehal.joshi@vita.com', '9820022222', 'Counselor', 'BMS', 2.0, 'https://i.pravatar.cc/300?img=32', 'snehal', '$2a$10$placeholderhash2', 1),
  (3, 'Dr. Ravi Kumar', 'ravi.kumar@vita.com', '9820033333', 'Faculty', 'PhD Computer Science', 12.0, 'https://i.pravatar.cc/300?img=12', 'ravi', '$2a$10$placeholderhash3', 1),
  (4, 'Ananya Desai', 'ananya.desai@vita.com', '9820044444', 'Faculty', 'M.Tech IT', 8.0, 'https://i.pravatar.cc/300?img=45', 'ananya', '$2a$10$placeholderhash4', 1),
  (5, 'Vikram Nair', 'vikram.nair@vita.com', '9820055555', 'Faculty', 'M.Sc Computer Science', 6.5, 'https://i.pravatar.cc/300?img=15', 'vikram', '$2a$10$placeholderhash5', 1),
  (6, 'Admin User', 'admin@vita.com', '9820000000', 'Admin', NULL, NULL, NULL, 'admin', '$2a$10$placeholderhash6', 1);

-- ============================================================
-- Courses
-- ============================================================
INSERT INTO courses (course_id, category_id, name, description, duration, fees, level, syllabus_url, cover_photo, is_active) VALUES
  (1, 1, 'PG-DAC', 'Post Graduate Diploma in Advanced Computing — C-DAC''s flagship program covering full-stack development, cloud, and enterprise systems.', '6 months', 90000.00, 'Intermediate', NULL, 'https://placehold.co/600x400?text=PG-DAC', 1),
  (2, 1, 'PG-DBDA', 'Post Graduate Diploma in Big Data Analytics — covers Hadoop, Spark, and machine learning pipelines.', '6 months', 95000.00, 'Advanced', NULL, 'https://placehold.co/600x400?text=PG-DBDA', 1),
  (3, 2, 'Java Full Stack Development', 'Hands-on certification covering Java, Spring Boot, React, and MySQL.', '3 months', 35000.00, 'Beginner', NULL, 'https://placehold.co/600x400?text=Java+Full+Stack', 1);

-- ============================================================
-- Course-Staff (primary faculty per course)
-- ============================================================
INSERT INTO course_staff (course_id, staff_id, is_primary) VALUES
  (1, 3, 1),
  (1, 4, 0),
  (2, 4, 1),
  (3, 5, 1);

-- ============================================================
-- Recruiters
-- ============================================================
INSERT INTO recruiters (recruiter_id, company_name, logo_url, is_active) VALUES
  (1, 'TCS', 'https://placehold.co/200x100?text=TCS', 1),
  (2, 'Infosys', 'https://placehold.co/200x100?text=Infosys', 1),
  (3, 'Capgemini', 'https://placehold.co/200x100?text=Capgemini', 1),
  (4, 'Accenture', 'https://placehold.co/200x100?text=Accenture', 1),
  (5, 'Wipro', 'https://placehold.co/200x100?text=Wipro', 1);

-- ============================================================
-- Closure reasons
-- ============================================================
INSERT INTO closure_reasons (reason_id, reason_text, is_active) VALUES
  (1, 'No Response After Follow-up', 1),
  (2, 'Not Interested', 1),
  (3, 'Chose Another Institute', 1),
  (4, 'Budget Constraint', 1);

-- ============================================================
-- Announcements (homepage ticker)
-- ============================================================
INSERT INTO announcements (content, start_date, end_date, display_order, is_active) VALUES
  ('PG-DAC Admissions Open for Aug 2026 batch!', NULL, NULL, 1, 1),
  ('Achieved 100% Placements for PG-DBDA Feb 2026 batch', NULL, NULL, 2, 1),
  ('New Java Full Stack certification batch starting soon — limited seats', NULL, NULL, 3, 1);

-- ============================================================
-- Banners
-- ============================================================
INSERT INTO banners (title, image_url, link_url, display_order, is_active) VALUES
  ('PG-DAC Admissions Open', 'https://placehold.co/1200x300?text=PG-DAC+Admissions+Open', '/courses/1', 1, 1),
  ('100% Placement Record', 'https://placehold.co/1200x300?text=100%25+Placements', '/placement/batchwise', 2, 1);

-- ============================================================
-- Testimonials
-- ============================================================
INSERT INTO testimonials (name, content, rating, photo_url, is_approved) VALUES
  ('Abhijeet Deokar', 'The PG-DAC program at VITA completely transformed my career. The faculty support and placement guidance were exceptional.', 5, 'https://i.pravatar.cc/300?img=51', 1),
  ('Adarsh Kodgire', 'Hands-on labs and mock interviews made all the difference. I landed a great offer within a month of course completion.', 5, 'https://i.pravatar.cc/300?img=53', 1),
  ('Ajay Polke', 'Great learning environment and industry-relevant curriculum. Highly recommend VITA for anyone serious about a tech career.', 4, 'https://i.pravatar.cc/300?img=56', 1);

-- ============================================================
-- News & Events
-- ============================================================
INSERT INTO news_events (title, content, image_url, event_date, is_active) VALUES
  ('Open House — August 2026', 'Join us for a campus tour and meet our faculty ahead of the new PG-DAC batch.', 'https://placehold.co/600x400?text=Open+House', '2026-08-15', 1),
  ('Guest Lecture: Cloud-Native Architecture', 'Industry expert session on modern cloud-native application design.', 'https://placehold.co/600x400?text=Guest+Lecture', '2026-08-05', 1);

-- ============================================================
-- Gallery images (Campus Life)
-- ============================================================
INSERT INTO gallery_images (title, description, image_url, category, is_active) VALUES
  ('Lab Session', 'Students working on a hands-on lab exercise', 'https://placehold.co/600x400?text=Lab+Session', 'Academics', 1),
  ('Guest Lecture', 'Industry expert delivering a session on cloud architecture', 'https://placehold.co/600x400?text=Guest+Lecture', 'Events', 1),
  ('Batch Photo', 'PG-DAC Aug 2024 batch group photo', 'https://placehold.co/600x400?text=Batch+Photo', 'Batches', 1),
  ('Mock Interview Day', 'Students participating in mock interview sessions', 'https://placehold.co/600x400?text=Mock+Interviews', 'Placement Prep', 1);

-- ============================================================
-- Inquiries (needed as FK for students)
-- ============================================================
INSERT INTO inquiries (inquiry_id, course_id, staff_id, enquirer_name, email, phone, source, status, inquiry_date, closure_reason_id) VALUES
  (1, 1, 1, 'Rahul Mehta', 'rahul.mehta@example.com', '9876543210', 'Online', 'Converted', '2024-07-01', NULL),
  (2, 2, 1, 'Sneha Patil', 'sneha.patil@example.com', '9876543211', 'Walk-in', 'Converted', '2024-07-03', NULL),
  (3, 1, 2, 'Arjun Rao', 'arjun.rao@example.com', '9876543212', 'Online', 'Converted', '2024-07-05', NULL),
  (4, 3, 2, 'Kavya Iyer', 'kavya.iyer@example.com', '9876543213', 'eMail', 'In-Followup', '2026-07-20', NULL),
  (5, 1, 1, 'Rohan Shah', 'rohan.shah@example.com', '9876543214', 'Online', 'Not Interested', '2026-06-10', 4);

-- ============================================================
-- Students (linked to converted inquiries)
-- ============================================================
INSERT INTO students (student_id, inquiry_id, first_name, last_name, parent_name, parent_phone, email, phone, dob, gender, address_line1, city, state, pincode, photo_url, qualification, reg_date) VALUES
  (1, 1, 'Rahul', 'Mehta', 'Suresh Mehta', '9820099991', 'rahul.mehta@example.com', '9876543210', '2000-05-14', 'Male', '12 MG Road', 'Mumbai', 'Maharashtra', '400001', 'https://i.pravatar.cc/300?img=11', 'B.E. Computer Engineering', '2024-07-10'),
  (2, 2, 'Sneha', 'Patil', 'Ramesh Patil', '9820099992', 'sneha.patil@example.com', '9876543211', '2001-02-20', 'Female', '45 Andheri West', 'Mumbai', 'Maharashtra', '400058', 'https://i.pravatar.cc/300?img=25', 'B.Sc IT', '2024-07-12'),
  (3, 3, 'Arjun', 'Rao', 'Krishna Rao', '9820099993', 'arjun.rao@example.com', '9876543212', '1999-11-30', 'Male', '78 Juhu Scheme', 'Mumbai', 'Maharashtra', '400049', 'https://i.pravatar.cc/300?img=13', 'BCA', '2024-07-14');

-- ============================================================
-- Batches (marked Completed so they show on the Placement pages)
-- ============================================================
INSERT INTO batches (batch_id, course_id, staff_id, batch_name, academic_year, start_date, end_date, timing, capacity, current_count, status, is_active) VALUES
  (1, 1, 3, 'PG-DAC-Aug-2024', '2024-25', '2024-08-01', '2025-01-31', '9 AM - 5 PM', 30, 2, 'Completed', 1),
  (2, 2, 4, 'PG-DBDA-Aug-2024', '2024-25', '2024-08-01', '2025-01-31', '9 AM - 5 PM', 25, 1, 'Completed', 1);

-- ============================================================
-- Enrollments
-- ============================================================
INSERT INTO enrollments (student_id, batch_id, inquiry_id, enroll_date, status) VALUES
  (1, 1, 1, '2024-08-01', 'Completed'),
  (3, 1, 3, '2024-08-01', 'Completed'),
  (2, 2, 2, '2024-08-01', 'Completed');

-- ============================================================
-- Payments (2 installments each, per the fixed-installment business rule)
-- ============================================================
INSERT INTO payments (student_id, enrollment_id, amount, installment_number, total_installments, payment_date, payment_mode, payment_status, receipt_no) VALUES
  (1, 1, 45000.00, 1, 2, '2024-08-01', 'UPI', 'Success', 'RCPT-0001'),
  (1, 1, 45000.00, 2, 2, '2024-10-01', 'UPI', 'Success', 'RCPT-0002'),
  (3, 2, 45000.00, 1, 2, '2024-08-01', 'Bank Transfer', 'Success', 'RCPT-0003'),
  (2, 3, 47500.00, 1, 2, '2024-08-01', 'Card', 'Success', 'RCPT-0004');

-- ============================================================
-- Placement records
-- ============================================================
INSERT INTO placement_records (student_id, batch_id, recruiter_id, position, package, placement_date, is_featured) VALUES
  (1, 1, 1, 'Software Engineer', 750000.00, '2025-02-10', 1),
  (3, 1, 4, 'Associate Software Engineer', 680000.00, '2025-02-15', 0),
  (2, 2, 2, 'Data Analyst', 700000.00, '2025-02-12', 1);
