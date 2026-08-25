package com.smvita.computerseekho.exception;

/**
 * For rules the DB schema can't enforce by itself — e.g. "closure_reason_id
 * is mandatory when status is Lost/Not Interested" or "course_staff can
 * only have one is_primary=1 row per course". Thrown from the Service
 * layer; the DB constraints (NOT NULL, FK, UNIQUE) handle everything that
 * can be expressed structurally.
 */
public class BusinessRuleException extends RuntimeException {
    public BusinessRuleException(String message) {
        super(message);
    }
}
