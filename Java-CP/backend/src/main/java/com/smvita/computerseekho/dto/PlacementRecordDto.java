package com.smvita.computerseekho.dto;

import java.math.BigDecimal;
import java.time.LocalDate;

/**
 * Read-focused DTO for the public Placement pages — flattens student name
 * / photo and recruiter name onto the placement record itself, since the
 * Placement Detail page (BRD §4.1) and the Recruiter drill-down (Turn 4
 * requirement) both need exactly this shape.
 */
public record PlacementRecordDto(
        Integer placementId,
        Integer studentId,
        String studentName,
        String studentPhotoUrl,
        Integer batchId,
        String batchName,
        Integer recruiterId,
        String recruiterCompanyName,
        String position,
        BigDecimal packageAmount,
        LocalDate placementDate,
        Boolean isFeatured
) {}
