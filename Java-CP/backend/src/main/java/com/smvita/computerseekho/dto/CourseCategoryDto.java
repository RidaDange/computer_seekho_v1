package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record CourseCategoryDto(
        Integer categoryId,

        @NotBlank(message = "Category name is required")
        @Size(max = 100)
        String name,

        @Size(max = 50)
        String ageGroup,

        String description,
        Boolean isActive
) {}
