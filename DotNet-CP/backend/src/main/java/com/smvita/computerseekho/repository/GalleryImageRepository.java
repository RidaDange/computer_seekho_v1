package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.GalleryImage;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface GalleryImageRepository extends JpaRepository<GalleryImage, Integer> {

    // Campus Life page consumes this.
    List<GalleryImage> findByIsActiveTrue();

    /**
     * Ordered so the theme grouping is stable between requests — without an
     * explicit order the "cover" for a theme could change on every reload,
     * which looks like a bug even though nothing changed.
     */
    List<GalleryImage> findByIsActiveTrueOrderByCategoryAscImageIdAsc();

    /** The drill-down. Case-insensitive because the theme arrives from a URL. */
    List<GalleryImage> findByCategoryIgnoreCaseAndIsActiveTrueOrderByImageIdAsc(String category);

    /** Photos with no theme set — grouped under a fallback rather than dropped. */
    List<GalleryImage> findByCategoryIsNullAndIsActiveTrueOrderByImageIdAsc();
}
