package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

/**
 * The enquiry form's payload, used by both entry channels — the public
 * website form and the staff "Add Enquiry" screen for walk-ins.
 *
 * Field-level validation is a KB requirement (§3.1), so the rules live on
 * the DTO where both channels inherit them, rather than being re-typed in
 * each React form.
 *
 * `staffId` is deliberately absent: the assigned counselor is decided by
 * the server's least-loaded round robin (KB §9.6), never posted by the
 * client.
 */
public record InquiryCreateRequest(

        @NotNull(message = "Please select a course")
        Integer courseId,

        @NotBlank(message = "Name is required")
        @Size(max = 150, message = "Name cannot exceed 150 characters")
        String enquirerName,

        @NotBlank(message = "Email is required")
        @Email(message = "Please enter a valid email address")
        @Size(max = 150, message = "Email cannot exceed 150 characters")
        String email,

        @NotBlank(message = "Phone number is required")
        @Pattern(regexp = "^[6-9]\\d{9}$",
                 message = "Please enter a valid 10-digit Indian mobile number")
        String phone,

        @Size(max = 2000, message = "Message is too long")
        String message,

        /** Website / Walk-in / eMail / Phone — defaulted per channel if blank. */
        @Size(max = 100)
        String source
) {}
