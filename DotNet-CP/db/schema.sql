-- ============================================================
--  ComputerSeekho — Final Schema (v4)
--  Consolidates every Knowledge Base decision through Turn 5:
--   - Removed courses.course_fees_from / course_fees_to
--   - course_staff.is_primary is the authoritative default-faculty source
--     (uniqueness enforced at the Service layer, not the DB — MySQL can't
--     express "at most one is_primary=1 per course" as a plain constraint)
--   - placement_drives / placement_records reference recruiters via a
--     recruiter_id FK — no more free-text company_name
--   - New tables: recruiters, closure_reasons, announcements
--   - inquiries.closure_reason_id added (app-mandatory when status is
--     'Lost' or 'Not Interested')
--   - students.inquiry_id and enrollments.inquiry_id are now NOT NULL
--     ("no enquiry, no registration")
--   - batch_albums.cover_image_id references batch_album_images directly
--     (my recommendation from Turn 4 — flagging again: easy to revert to
--     a plain URL column if you'd rather allow a separately-uploaded cover)
-- ============================================================

CREATE DATABASE IF NOT EXISTS computerseekho
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE computerseekho;

-- ============================================================
-- 1. COURSE_CATEGORIES
-- ============================================================
CREATE TABLE course_categories (
  category_id   INT          NOT NULL AUTO_INCREMENT,
  name          VARCHAR(100) NOT NULL UNIQUE,
  age_group     VARCHAR(50),
  description   TEXT,
  is_active     TINYINT(1)   NOT NULL DEFAULT 1,
  PRIMARY KEY (category_id)
);

-- ============================================================
-- 2. STAFF
-- ============================================================
CREATE TABLE staff (
  staff_id      INT          NOT NULL AUTO_INCREMENT,
  name          VARCHAR(150) NOT NULL,
  email         VARCHAR(150) NOT NULL UNIQUE,
  phone         VARCHAR(15),
  role          ENUM('Admin','Counselor','Faculty','Manager','Receptionist') NOT NULL DEFAULT 'Counselor',
  qualification VARCHAR(200),
  experience    DECIMAL(4,1) COMMENT 'Years of experience',
  photo_url     VARCHAR(500),
  username      VARCHAR(50)  NOT NULL UNIQUE,
  password_hash VARCHAR(255) NOT NULL,
  is_active     TINYINT(1)   NOT NULL DEFAULT 1,
  PRIMARY KEY (staff_id)
);

-- ============================================================
-- 3. COURSES  (course_fees_from / course_fees_to removed per Turn 4)
-- ============================================================
CREATE TABLE courses (
  course_id     INT           NOT NULL AUTO_INCREMENT,
  category_id   INT           NOT NULL,
  name          VARCHAR(150)  NOT NULL,
  description   TEXT,
  duration      VARCHAR(50),
  fees          DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  level         ENUM('Beginner','Intermediate','Advanced') NOT NULL DEFAULT 'Beginner',
  syllabus_url  VARCHAR(500),
  cover_photo   VARCHAR(255),
  is_active     TINYINT(1)    NOT NULL DEFAULT 1,
  PRIMARY KEY (course_id),
  CONSTRAINT fk_course_category FOREIGN KEY (category_id) REFERENCES course_categories(category_id)
    ON DELETE RESTRICT ON UPDATE CASCADE
);

-- ============================================================
-- 4. COURSE_STAFF (junction; is_primary = authoritative default faculty)
-- ============================================================
CREATE TABLE course_staff (
  course_staff_id INT NOT NULL AUTO_INCREMENT,
  course_id       INT NOT NULL,
  staff_id        INT NOT NULL,
  assigned_date   DATE DEFAULT (CURRENT_DATE),
  is_primary      TINYINT(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (course_staff_id),
  CONSTRAINT fk_course_staff_course FOREIGN KEY (course_id) REFERENCES courses(course_id)
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT fk_course_staff_staff FOREIGN KEY (staff_id) REFERENCES staff(staff_id)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT uk_course_staff UNIQUE (course_id, staff_id)
);

-- ============================================================
-- 5. BATCHES
-- ============================================================
CREATE TABLE batches (
  batch_id          INT          NOT NULL AUTO_INCREMENT,
  course_id         INT          NOT NULL,
  staff_id          INT          NOT NULL,
  batch_name        VARCHAR(100) NOT NULL,
  academic_year     VARCHAR(20)  COMMENT 'e.g. "2024-25"',
  start_date        DATE,
  end_date          DATE,
  presentation_date DATETIME     NULL,
  timing            VARCHAR(100),
  capacity          INT          NOT NULL DEFAULT 20,
  current_count     INT          NOT NULL DEFAULT 0 COMMENT 'System-calculated from enrollments; never hand-edited via Table Maintenance',
  status            ENUM('Upcoming','Ongoing','Completed','Cancelled') NOT NULL DEFAULT 'Upcoming',
  is_active         TINYINT(1)   NOT NULL DEFAULT 1,
  PRIMARY KEY (batch_id),
  CONSTRAINT fk_batch_course FOREIGN KEY (course_id) REFERENCES courses(course_id)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT fk_batch_staff FOREIGN KEY (staff_id) REFERENCES staff(staff_id)
    ON DELETE RESTRICT ON UPDATE CASCADE
);

-- ============================================================
-- 6. RECRUITERS (new — single company master; powers Our Recruiters page,
--    placement_drives, and placement_records)
-- ============================================================
CREATE TABLE recruiters (
  recruiter_id  INT          NOT NULL AUTO_INCREMENT,
  company_name  VARCHAR(150) NOT NULL UNIQUE,
  logo_url      VARCHAR(500) NULL,
  is_active     TINYINT(1)   NOT NULL DEFAULT 1 COMMENT 'Controls visibility on the public Our Recruiters page only; historical placements remain intact regardless',
  PRIMARY KEY (recruiter_id)
);

-- ============================================================
-- 7. CLOSURE_REASONS (new — standard dropdown for closing an enquiry)
-- ============================================================
CREATE TABLE closure_reasons (
  reason_id     INT          NOT NULL AUTO_INCREMENT,
  reason_text   VARCHAR(200) NOT NULL UNIQUE,
  is_active     TINYINT(1)   NOT NULL DEFAULT 1,
  PRIMARY KEY (reason_id)
);

-- ============================================================
-- 8. INQUIRIES
-- ============================================================
CREATE TABLE inquiries (
  inquiry_id        INT          NOT NULL AUTO_INCREMENT,
  course_id         INT          NOT NULL,
  staff_id          INT,
  enquirer_name     VARCHAR(150) NOT NULL,
  email             VARCHAR(150),
  phone             VARCHAR(15),
  message           TEXT,
  source            VARCHAR(100),
  status            ENUM('New','In-Followup','Converted','Lost','Not Interested') NOT NULL DEFAULT 'New',
  inquiry_date      DATE         NOT NULL DEFAULT (CURRENT_DATE),
  closure_reason_id INT NULL COMMENT 'App-mandatory when status = Lost or Not Interested; not required for Converted',
  PRIMARY KEY (inquiry_id),
  CONSTRAINT fk_inquiry_course FOREIGN KEY (course_id) REFERENCES courses(course_id)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT fk_inquiry_staff FOREIGN KEY (staff_id) REFERENCES staff(staff_id)
    ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT fk_inquiry_closure_reason FOREIGN KEY (closure_reason_id) REFERENCES closure_reasons(reason_id)
    ON DELETE RESTRICT ON UPDATE CASCADE
);

-- ============================================================
-- 9. STUDENTS  (inquiry_id now mandatory — "no enquiry, no registration")
-- ============================================================
CREATE TABLE students (
  student_id     INT          NOT NULL AUTO_INCREMENT,
  inquiry_id     INT          NOT NULL,
  first_name     VARCHAR(150) NOT NULL,
  last_name      VARCHAR(150) NOT NULL,
  parent_name    VARCHAR(150) NOT NULL,
  parent_phone   VARCHAR(15),
  email          VARCHAR(150) UNIQUE,
  phone          VARCHAR(15),
  dob            DATE,
  gender         ENUM('Male','Female','Other'),
  address_line1  VARCHAR(255) NULL,
  address_line2  VARCHAR(255) NULL,
  city           VARCHAR(100) NULL,
  state          VARCHAR(100) NULL,
  pincode        VARCHAR(10)  NULL,
  photo_url      VARCHAR(500),
  qualification  VARCHAR(150) NULL,
  reg_date       DATE         NOT NULL DEFAULT (CURRENT_DATE),
  PRIMARY KEY (student_id),
  CONSTRAINT fk_student_inquiry FOREIGN KEY (inquiry_id) REFERENCES inquiries(inquiry_id)
    ON DELETE RESTRICT ON UPDATE CASCADE
);

-- ============================================================
-- 10. FOLLOWUPS
-- ============================================================
CREATE TABLE followups (
  followup_id    INT  NOT NULL AUTO_INCREMENT,
  inquiry_id     INT  NOT NULL,
  staff_id       INT  NOT NULL,
  followup_date  DATE NOT NULL DEFAULT (CURRENT_DATE),
  notes          TEXT,
  next_followup  DATE,
  status         ENUM('Pending','Done','No Response') NOT NULL DEFAULT 'Pending',
  PRIMARY KEY (followup_id),
  CONSTRAINT fk_followup_inquiry FOREIGN KEY (inquiry_id) REFERENCES inquiries(inquiry_id)
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT fk_followup_staff FOREIGN KEY (staff_id) REFERENCES staff(staff_id)
    ON DELETE RESTRICT ON UPDATE CASCADE
);

-- ============================================================
-- 11. ENROLLMENTS  (inquiry_id now mandatory)
-- ============================================================
CREATE TABLE enrollments (
  enrollment_id INT  NOT NULL AUTO_INCREMENT,
  student_id    INT  NOT NULL,
  batch_id      INT  NOT NULL,
  inquiry_id    INT  NOT NULL,
  enroll_date   DATE NOT NULL DEFAULT (CURRENT_DATE),
  status        ENUM('Active','Completed','Dropped') NOT NULL DEFAULT 'Active',
  PRIMARY KEY (enrollment_id),
  CONSTRAINT fk_enroll_student FOREIGN KEY (student_id) REFERENCES students(student_id)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT fk_enroll_batch FOREIGN KEY (batch_id) REFERENCES batches(batch_id)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT fk_enroll_inquiry FOREIGN KEY (inquiry_id) REFERENCES inquiries(inquiry_id)
    ON DELETE RESTRICT ON UPDATE CASCADE
);

-- ============================================================
-- 12. PAYMENTS  (fixed at 2 installments by business rule; schema stays general)
-- ============================================================
CREATE TABLE payments (
  payment_id          INT            NOT NULL AUTO_INCREMENT,
  student_id          INT            NOT NULL,
  enrollment_id       INT            NOT NULL,
  amount              DECIMAL(10,2)  NOT NULL,
  installment_number  INT            NOT NULL DEFAULT 1,
  total_installments  INT            NOT NULL DEFAULT 2 COMMENT 'Business rule: fixed at 2 installments',
  payment_date        DATE           NOT NULL DEFAULT (CURRENT_DATE),
  payment_mode        ENUM('Cash','UPI','Card','Bank Transfer','Cheque') NOT NULL DEFAULT 'Cash',
  payment_status      ENUM('Success','Pending','Failed','Refunded') NOT NULL DEFAULT 'Success',
  transaction_id      VARCHAR(100),
  receipt_no          VARCHAR(100)   NOT NULL UNIQUE,
  remarks             TEXT,
  PRIMARY KEY (payment_id),
  CONSTRAINT fk_payment_student FOREIGN KEY (student_id) REFERENCES students(student_id)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT fk_payment_enrollment FOREIGN KEY (enrollment_id) REFERENCES enrollments(enrollment_id)
    ON DELETE RESTRICT ON UPDATE CASCADE
);

-- ============================================================
-- 13. PLACEMENT_DRIVES  (DDL syntax fixed; recruiter_id FK replaces free-text company_name)
-- ============================================================
CREATE TABLE placement_drives (
  drive_id                 INT            NOT NULL AUTO_INCREMENT,
  recruiter_id              INT            NOT NULL,
  course_id                 INT            NULL,
  drive_date                 DATE           NOT NULL,
  drive_mode                 ENUM('Online','Offline','Hybrid') NOT NULL,
  position                    VARCHAR(100)   NOT NULL,
  description                 TEXT           NULL,
  eligibility_criteria       TEXT           NULL,
  package                     DECIMAL(10,2)  NULL COMMENT 'Annual CTC in INR',
  hr_contact_name             VARCHAR(150)   NULL,
  hr_contact_email            VARCHAR(150)   NULL,
  hr_contact_phone            VARCHAR(15)    NULL,
  no_of_openings              INT            NULL,
  no_of_students_selected     INT            NULL,
  drive_status                 ENUM('Scheduled','Completed','Cancelled') NOT NULL DEFAULT 'Scheduled',
  PRIMARY KEY (drive_id),
  CONSTRAINT fk_drive_recruiter FOREIGN KEY (recruiter_id) REFERENCES recruiters(recruiter_id)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT fk_drive_course FOREIGN KEY (course_id) REFERENCES courses(course_id)
    ON DELETE SET NULL ON UPDATE CASCADE
);

-- ============================================================
-- 14. PLACEMENT_RECORDS  (recruiter_id FK replaces free-text company_name)
-- ============================================================
CREATE TABLE placement_records (
  placement_id   INT            NOT NULL AUTO_INCREMENT,
  student_id     INT            NOT NULL,
  batch_id       INT,
  recruiter_id   INT            NOT NULL,
  position       VARCHAR(200),
  drive_id       INT,
  package        DECIMAL(10,2)  COMMENT 'Annual CTC in INR',
  placement_date DATE,
  is_featured    TINYINT(1)     NOT NULL DEFAULT 0,
  PRIMARY KEY (placement_id),
  CONSTRAINT fk_placement_student FOREIGN KEY (student_id) REFERENCES students(student_id)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT fk_placement_batch FOREIGN KEY (batch_id) REFERENCES batches(batch_id)
    ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT fk_placement_recruiter FOREIGN KEY (recruiter_id) REFERENCES recruiters(recruiter_id)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT fk_placement_drive FOREIGN KEY (drive_id) REFERENCES placement_drives(drive_id)
    ON DELETE SET NULL ON UPDATE CASCADE
);

-- ============================================================
-- 15. BATCH_ALBUMS  (cover_image_id FK wired up after batch_album_images exists — see bottom)
-- ============================================================
CREATE TABLE batch_albums (
  album_id       INT          NOT NULL AUTO_INCREMENT,
  batch_id       INT          NOT NULL,
  title          VARCHAR(200) NOT NULL DEFAULT 'Batch Photos',
  description    TEXT,
  cover_image_id INT NULL,
  created_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  is_active      TINYINT(1)   NOT NULL DEFAULT 1,
  PRIMARY KEY (album_id),
  CONSTRAINT fk_album_batch FOREIGN KEY (batch_id) REFERENCES batches(batch_id)
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT uk_batch_album UNIQUE (batch_id)
);

-- ============================================================
-- 16. BATCH_ALBUM_IMAGES
-- ============================================================
CREATE TABLE batch_album_images (
  image_id      INT          NOT NULL AUTO_INCREMENT,
  album_id      INT          NOT NULL,
  image_url     VARCHAR(500) NOT NULL,
  caption       VARCHAR(255),
  uploaded_by   INT,
  upload_date   DATE         NOT NULL DEFAULT (CURRENT_DATE),
  display_order INT          NOT NULL DEFAULT 0,
  is_active     TINYINT(1)   NOT NULL DEFAULT 1,
  PRIMARY KEY (image_id),
  CONSTRAINT fk_albumimg_album FOREIGN KEY (album_id) REFERENCES batch_albums(album_id)
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT fk_albumimg_staff FOREIGN KEY (uploaded_by) REFERENCES staff(staff_id)
    ON DELETE SET NULL ON UPDATE CASCADE
);

-- Wire up the cover-image FK now that batch_album_images exists
ALTER TABLE batch_albums
  ADD CONSTRAINT fk_album_cover_image FOREIGN KEY (cover_image_id) REFERENCES batch_album_images(image_id)
  ON DELETE SET NULL ON UPDATE CASCADE;

-- ============================================================
-- 17. GALLERY_IMAGES
-- ============================================================
CREATE TABLE gallery_images (
  image_id    INT          NOT NULL AUTO_INCREMENT,
  title       VARCHAR(200) NOT NULL,
  description TEXT,
  image_url   VARCHAR(500) NOT NULL,
  category    VARCHAR(100),
  upload_date DATE         NOT NULL DEFAULT (CURRENT_DATE),
  is_active   TINYINT(1)   NOT NULL DEFAULT 1,
  PRIMARY KEY (image_id)
);

-- ============================================================
-- 18. TESTIMONIALS
-- ============================================================
CREATE TABLE testimonials (
  testimonial_id INT          NOT NULL AUTO_INCREMENT,
  name           VARCHAR(150) NOT NULL,
  content        TEXT         NOT NULL,
  rating         TINYINT      CHECK (rating BETWEEN 1 AND 5),
  photo_url      VARCHAR(500),
  is_approved    TINYINT(1)   NOT NULL DEFAULT 0,
  created_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (testimonial_id)
);

-- ============================================================
-- 19. NEWS_EVENTS
-- ============================================================
CREATE TABLE news_events (
  news_id     INT          NOT NULL AUTO_INCREMENT,
  title       VARCHAR(200) NOT NULL,
  content     TEXT,
  image_url   VARCHAR(500),
  event_date  DATE,
  is_active   TINYINT(1)   NOT NULL DEFAULT 1,
  created_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (news_id)
);

-- ============================================================
-- 20. BANNERS
-- ============================================================
CREATE TABLE banners (
  banner_id     INT          NOT NULL AUTO_INCREMENT,
  title         VARCHAR(200),
  image_url     VARCHAR(500) NOT NULL,
  link_url      VARCHAR(500),
  display_order INT          NOT NULL DEFAULT 0,
  is_active     TINYINT(1)   NOT NULL DEFAULT 1,
  start_date    DATE,
  end_date      DATE,
  PRIMARY KEY (banner_id)
);

-- ============================================================
-- 21. ANNOUNCEMENTS (homepage crawling-text ticker)
-- ============================================================
CREATE TABLE announcements (
  announcement_id INT          NOT NULL AUTO_INCREMENT,
  content          VARCHAR(500) NOT NULL,
  start_date       DATE,
  end_date         DATE,
  display_order    INT          NOT NULL DEFAULT 0,
  is_active        TINYINT(1)   NOT NULL DEFAULT 1,
  created_at       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (announcement_id)
);

-- ============================================================
-- 22. CONTACT_MESSAGES (new — Get in Touch form submissions)
-- ============================================================
CREATE TABLE contact_messages (
  message_id    INT          NOT NULL AUTO_INCREMENT,
  name          VARCHAR(150) NOT NULL,
  email         VARCHAR(150) NOT NULL,
  message       VARCHAR(500) NOT NULL,
  is_read       TINYINT(1)   NOT NULL DEFAULT 0,
  created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (message_id)
);

