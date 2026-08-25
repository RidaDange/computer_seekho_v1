package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.GalleryCategoryDto;
import com.smvita.computerseekho.dto.GalleryImageDto;
import com.smvita.computerseekho.entity.GalleryImage;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.GalleryImageRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

@Service
@RequiredArgsConstructor
@Transactional
public class GalleryImageService {

    private final GalleryImageRepository galleryImageRepository;

    public List<GalleryImageDto> findAll() {
        return galleryImageRepository.findAll().stream().map(this::toDto).toList();
    }

    public List<GalleryImageDto> findActive() {
        return galleryImageRepository.findByIsActiveTrue().stream().map(this::toDto).toList();
    }

    /** Fallback bucket for photos saved without a theme, so none go missing. */
    public static final String UNCATEGORISED = "Other";

    /**
     * The clickable themes on Campus Life.
     *
     * Grouped in the service rather than by a GROUP BY: the page needs a
     * cover image per theme as well as a count, and picking "the first
     * photo" is an ordering decision, not an aggregate. The gallery is a
     * few dozen rows at most, so a single ordered read costs nothing.
     */
    @Transactional(readOnly = true)
    public List<GalleryCategoryDto> findCategories() {
        Map<String, List<GalleryImage>> byTheme = new LinkedHashMap<>();

        for (GalleryImage img : galleryImageRepository.findByIsActiveTrueOrderByCategoryAscImageIdAsc()) {
            String theme = img.getCategory() == null || img.getCategory().isBlank()
                    ? UNCATEGORISED
                    : img.getCategory().trim();
            byTheme.computeIfAbsent(theme, k -> new ArrayList<>()).add(img);
        }

        List<GalleryCategoryDto> categories = new ArrayList<>();
        byTheme.forEach((theme, images) ->
                categories.add(new GalleryCategoryDto(theme, images.get(0).getImageUrl(), images.size())));
        return categories;
    }

    /** Every photo in one theme — the drill-down behind a Campus Life tile. */
    @Transactional(readOnly = true)
    public List<GalleryImageDto> findByCategory(String category) {
        List<GalleryImage> images = UNCATEGORISED.equalsIgnoreCase(category)
                ? galleryImageRepository.findByCategoryIsNullAndIsActiveTrueOrderByImageIdAsc()
                : galleryImageRepository.findByCategoryIgnoreCaseAndIsActiveTrueOrderByImageIdAsc(category);
        return images.stream().map(this::toDto).toList();
    }

    public GalleryImageDto findById(Integer id) {
        return toDto(getEntityOrThrow(id));
    }

    public GalleryImageDto create(GalleryImageDto dto) {
        GalleryImage image = new GalleryImage();
        applyDto(image, dto);
        return toDto(galleryImageRepository.save(image));
    }

    public GalleryImageDto update(Integer id, GalleryImageDto dto) {
        GalleryImage image = getEntityOrThrow(id);
        applyDto(image, dto);
        return toDto(galleryImageRepository.save(image));
    }

    public void delete(Integer id) {
        galleryImageRepository.delete(getEntityOrThrow(id));
    }

    private GalleryImage getEntityOrThrow(Integer id) {
        return galleryImageRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Gallery Image", id));
    }

    private void applyDto(GalleryImage img, GalleryImageDto dto) {
        img.setTitle(dto.title());
        img.setDescription(dto.description());
        img.setImageUrl(dto.imageUrl());
        img.setCategory(dto.category());
        if (dto.uploadDate() != null) {
            img.setUploadDate(dto.uploadDate());
        }
        img.setIsActive(dto.isActive() != null ? dto.isActive() : true);
    }

    private GalleryImageDto toDto(GalleryImage img) {
        return new GalleryImageDto(img.getImageId(), img.getTitle(), img.getDescription(),
                img.getImageUrl(), img.getCategory(), img.getUploadDate(), img.getIsActive());
    }
}
