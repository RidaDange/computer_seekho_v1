package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Positive;
import jakarta.validation.constraints.Size;

import java.time.LocalDate;
import java.time.LocalDateTime;

/**
 * The full batch record, used by the dedicated Batch Management screen
 * (KB §9.1 — batches are deliberately excluded from the generic Table
 * Maintenance grid because current_count and status are system-driven).
 *
 * currentCount is read-only on the way in: it's recomputed from
 * enrollments on every registration, so accepting it from a form would let
 * a typo desynchronise the roster from the headcount.
 */
public record BatchDetailDto(
        Integer batchId,

        @NotNull(message = "Course is required")
        Integer courseId,
        String courseName,

        @NotNull(message = "A faculty member must own the batch")
        Integer staffId,
        String staffName,

        @NotBlank(message = "Batch name is required")
        @Size(max = 100)
        String batchName,

        @Size(max = 20)
        String academicYear,

        LocalDate startDate,
        LocalDate endDate,
        LocalDateTime presentationDate,

        @Size(max = 100)
        String timing,

        @NotNull(message = "Capacity is required")
        @Positive(message = "Capacity must be greater than zero")
        Integer capacity,

        Integer currentCount,
        String status,   // Upcoming | Ongoing | Completed | Cancelled
        Boolean isActive
) {}
