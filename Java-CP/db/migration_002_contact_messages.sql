-- ============================================================
--  Migration: add contact_messages table
--  Reason: the BRD's "Get in Touch" contact form (Name, Email, Message)
--  is a general site-wide message, separate from the course-specific
--  `inquiries` table (which requires course_id and is tied to the
--  Inquiry -> Follow-up -> Registration CRM pipeline). Safe to run
--  against an existing database — only adds a new table, touches nothing
--  else.
-- ============================================================

USE computerseekho;

CREATE TABLE IF NOT EXISTS contact_messages (
  message_id    INT          NOT NULL AUTO_INCREMENT,
  name          VARCHAR(150) NOT NULL,
  email         VARCHAR(150) NOT NULL,
  message       VARCHAR(500) NOT NULL,
  is_read       TINYINT(1)   NOT NULL DEFAULT 0,
  created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (message_id)
);
