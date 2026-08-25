package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.NotBlank;

import java.time.LocalDate;

public record GalleryImageDto(
        Integer imageId,

        @NotBlank(message = "Title is required")
        String title,

        String description,

        @NotBlank(message = "Image URL is required")
        String imageUrl,

        String category,
        LocalDate uploadDate,
        Boolean isActive
) {}
