package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.NotBlank;

import java.time.LocalDate;

public record BannerDto(
        Integer bannerId,
        String title,

        @NotBlank(message = "Image URL is required")
        String imageUrl,

        String linkUrl,
        Integer displayOrder,
        Boolean isActive,
        LocalDate startDate,
        LocalDate endDate
) {}
