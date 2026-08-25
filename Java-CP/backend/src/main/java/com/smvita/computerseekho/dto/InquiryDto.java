package com.smvita.computerseekho.dto;

import java.time.LocalDate;

/**
 * Read model for an enquiry. Carries the resolved course / counselor /
 * closure-reason names so the Follow-up and Enquiry lists render without
 * a lookup call per row, plus `nextFollowupDate` lifted from the enquiry's
 * open follow-up so the list can show what's coming without a second call.
 */
public record InquiryDto(
        Integer inquiryId,
        Integer courseId,
        String courseName,
        Integer staffId,
        String staffName,
        String enquirerName,
        String email,
        String phone,
        String message,
        String source,
        String status,
        LocalDate inquiryDate,
        Integer closureReasonId,
        String closureReasonText,
        LocalDate nextFollowupDate
) {}
