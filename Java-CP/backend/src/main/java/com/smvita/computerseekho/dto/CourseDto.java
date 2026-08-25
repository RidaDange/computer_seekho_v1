package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

import java.math.BigDecimal;

public record CourseDto(
        Integer courseId,

        @NotNull(message = "Category is required")
        Integer categoryId,
        String categoryName, // read-only, resolved for display — ignored on write

        @NotBlank(message = "Course name is required")
        String name,

        String description,
        String duration,

        @NotNull(message = "Fees are required")
        BigDecimal fees,

        String level, // Beginner | Intermediate | Advanced
        String syllabusUrl,
        String coverPhoto,
        Boolean isActive,

        // Resolved via course_staff.is_primary = true — read-only.
        // Set/changed through CourseController's dedicated
        // /courses/{id}/primary-faculty/{staffId} endpoint, not here.
        Integer primaryFacultyId,
        String primaryFacultyName
) {}
