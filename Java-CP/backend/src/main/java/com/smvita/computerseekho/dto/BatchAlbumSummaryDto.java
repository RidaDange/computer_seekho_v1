package com.smvita.computerseekho.dto;

/**
 * One card on the public Batch Albums strip. Deliberately lighter than
 * BatchAlbumDto: the listing needs a cover and a count, not every image
 * row for every album on the page.
 */
public record BatchAlbumSummaryDto(
        Integer albumId,
        Integer batchId,
        String batchName,
        String courseName,
        String academicYear,
        String title,
        String description,
        String coverImageUrl,
        int photoCount
) {}
