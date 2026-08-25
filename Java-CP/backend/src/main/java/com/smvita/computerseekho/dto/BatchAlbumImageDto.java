package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

import java.time.LocalDate;

public record BatchAlbumImageDto(
        Integer imageId,

        @NotBlank(message = "An image URL is required")
        @Size(max = 500)
        String imageUrl,

        @Size(max = 255)
        String caption,

        LocalDate uploadDate,
        Integer displayOrder,
        Boolean isActive,
        /** True for the one image the album uses as its cover. */
        Boolean isCover
) {}
