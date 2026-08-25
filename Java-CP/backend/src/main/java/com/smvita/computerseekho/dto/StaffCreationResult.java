package com.smvita.computerseekho.dto;

/**
 * Returned ONLY from the create-staff endpoint. temporaryPassword is
 * plaintext and shown exactly once — the admin screen should display it
 * prominently ("share this with the new staff member") and never store
 * or log it anywhere after this response.
 */
public record StaffCreationResult(StaffDto staff, String temporaryPassword) {}
