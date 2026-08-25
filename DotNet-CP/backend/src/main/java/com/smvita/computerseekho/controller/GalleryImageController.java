package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.GalleryCategoryDto;
import com.smvita.computerseekho.dto.GalleryImageDto;
import com.smvita.computerseekho.service.GalleryImageService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/gallery-images")
@RequiredArgsConstructor
public class GalleryImageController {

    private final GalleryImageService galleryImageService;

    @GetMapping
    public List<GalleryImageDto> findAll() {
        return galleryImageService.findAll();
    }

    @GetMapping("/active")
    public List<GalleryImageDto> findActive() {
        return galleryImageService.findActive();
    }

    /** The clickable themes on Campus Life, each with a cover and a count. */
    @GetMapping("/categories")
    public List<GalleryCategoryDto> findCategories() {
        return galleryImageService.findCategories();
    }

    /**
     * Every photo in one theme. The theme name comes in as a path variable
     * and can contain spaces ("Lab Sessions"), which the client URL-encodes.
     */
    @GetMapping("/by-category/{category}")
    public List<GalleryImageDto> findByCategory(@PathVariable String category) {
        return galleryImageService.findByCategory(category);
    }

    @GetMapping("/{id}")
    public GalleryImageDto findById(@PathVariable Integer id) {
        return galleryImageService.findById(id);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public GalleryImageDto create(@Valid @RequestBody GalleryImageDto dto) {
        return galleryImageService.create(dto);
    }

    @PutMapping("/{id}")
    public GalleryImageDto update(@PathVariable Integer id, @Valid @RequestBody GalleryImageDto dto) {
        return galleryImageService.update(id, dto);
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable Integer id) {
        galleryImageService.delete(id);
    }
}
