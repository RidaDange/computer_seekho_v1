package com.smvita.computerseekho.dto;

/**
 * One clickable theme on the Campus Life page — "Lab Sessions",
 * "Guest Lectures" and so on.
 *
 * Derived from gallery_images.category rather than stored in its own
 * table: a theme has no attributes of its own beyond its name, so a
 * lookup table would add a join and a maintenance screen without adding
 * any information.
 */
public record GalleryCategoryDto(
        String category,
        String coverImageUrl,
        int photoCount
) {}
