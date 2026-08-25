package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.NotBlank;

import java.time.LocalDate;

public record NewsEventDto(
        Integer newsId,

        @NotBlank(message = "Title is required")
        String title,

        String content,
        String imageUrl,
        LocalDate eventDate,
        Boolean isActive
) {}
