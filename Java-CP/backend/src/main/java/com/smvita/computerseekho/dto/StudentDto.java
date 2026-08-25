package com.smvita.computerseekho.dto;

import java.time.LocalDate;

/**
 * Read model for a registered student. Carries the originating enquiry and
 * the current batch so the Students list can show where someone came from
 * and where they ended up without a lookup per row.
 */
public record StudentDto(
        Integer studentId,
        Integer inquiryId,
        String firstName,
        String lastName,
        String parentName,
        String parentPhone,
        String email,
        String phone,
        LocalDate dob,
        String gender,
        String addressLine1,
        String addressLine2,
        String city,
        String state,
        String pincode,
        String photoUrl,
        String qualification,
        LocalDate regDate,
        Integer enrollmentId,
        Integer batchId,
        String batchName,
        String courseName
) {}
