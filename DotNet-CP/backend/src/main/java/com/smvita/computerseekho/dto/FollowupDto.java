package com.smvita.computerseekho.dto;

import java.time.LocalDate;

/**
 * Read model for the Follow-up working list. Flattened deliberately: the
 * counselor's screen needs the enquirer's name and phone right there in the
 * row to make the call, not behind a drill-down.
 */
public record FollowupDto(
        Integer followupId,
        Integer inquiryId,
        String enquirerName,
        String email,
        String phone,
        String courseName,
        Integer staffId,
        String staffName,
        LocalDate followupDate,
        String notes,
        LocalDate nextFollowup,
        String status,
        String inquiryStatus,
        /** Negative while still upcoming; > 0 once the follow-up is overdue. */
        long daysOverdue
) {}
