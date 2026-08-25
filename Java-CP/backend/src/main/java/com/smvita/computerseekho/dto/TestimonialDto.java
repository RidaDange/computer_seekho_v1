package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;

public record TestimonialDto(
        Integer testimonialId,

        @NotBlank(message = "Name is required")
        String name,

        @NotBlank(message = "Testimonial content is required")
        String content,

        @Min(1) @Max(5)
        Integer rating,

        String photoUrl,
        Boolean isApproved
) {}
