package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

import java.math.BigDecimal;
import java.time.LocalDate;

public record PlacementDriveDto(
        Integer driveId,

        @NotNull(message = "Recruiter is required")
        Integer recruiterId,
        String recruiterCompanyName,

        /** Optional — an open drive isn't tied to one course. */
        Integer courseId,
        String courseName,

        @NotNull(message = "Drive date is required")
        LocalDate driveDate,

        String driveMode, // Online | Offline | Hybrid

        @NotBlank(message = "Position is required")
        @Size(max = 100)
        String position,

        String description,
        String eligibilityCriteria,
        BigDecimal packageAmount,
        String hrContactName,
        String hrContactEmail,
        String hrContactPhone,
        Integer noOfOpenings,
        Integer noOfStudentsSelected,
        String driveStatus // Scheduled | Completed | Cancelled
) {}
