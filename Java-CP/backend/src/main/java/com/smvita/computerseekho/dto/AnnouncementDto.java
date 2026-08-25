package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

import java.time.LocalDate;

public record AnnouncementDto(
        Integer announcementId,

        @NotBlank(message = "Announcement text is required")
        @Size(max = 500)
        String content,

        LocalDate startDate,
        LocalDate endDate,
        Integer displayOrder,
        Boolean isActive
) {}
