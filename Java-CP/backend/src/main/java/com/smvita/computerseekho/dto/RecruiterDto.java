package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

/**
 * Backend requirement: "Use of DTO". Controllers never accept/return the
 * JPA entity directly — this is the shape every other master-table DTO
 * (AnnouncementDto, ClosureReasonDto, ...) mirrors.
 */
public record RecruiterDto(
        Integer recruiterId,

        @NotBlank(message = "Company name is required")
        @Size(max = 150)
        String companyName,

        @Size(max = 500)
        String logoUrl,

        Boolean isActive
) {}
