package com.smvita.computerseekho.dto;

public record BatchDto(
        Integer batchId,
        String batchName,
        Integer courseId,
        String courseName,
        Integer categoryId,
        String categoryName, // used to group batches into "PG-DAC Placement" / "PG-DBDA Placement" sections
        String academicYear,
        Integer capacity,
        Integer currentCount,
        String status,
        Boolean isActive,
        Long placedCount // resolved via PlacementRecordRepository — powers "X/Y placed"
) {}
