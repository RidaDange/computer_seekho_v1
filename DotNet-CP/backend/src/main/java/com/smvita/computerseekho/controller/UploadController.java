package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.UploadResultDto;
import com.smvita.computerseekho.service.FileStorageService;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.multipart.MultipartFile;

/**
 * One upload endpoint for every kind of image the admin panel handles.
 *
 * Deliberately not per-entity ("POST /students/{id}/photo"): the upload has
 * to happen while the form is still being filled in, before the student,
 * batch or staff row exists to attach it to. The client uploads first, gets a
 * URL back, and submits that URL with the rest of the form — which also means
 * a failed save doesn't lose the photo, and the existing create/update
 * endpoints don't each need a multipart variant.
 *
 * The trade-off is orphaned files when a form is abandoned. That's disk, not
 * data corruption, and a periodic sweep can reclaim it.
 */
@RestController
@RequestMapping("/uploads")
@RequiredArgsConstructor
public class UploadController {

    private final FileStorageService fileStorageService;

    /**
     * @param category one of students, staff, batches, courses, gallery,
     *                 banners, testimonials, recruiters — it becomes the
     *                 subdirectory, and anything else is rejected.
     */
    @PostMapping
    public UploadResultDto upload(@RequestParam("file") MultipartFile file,
                                  @RequestParam(defaultValue = "gallery") String category) {
        String url = fileStorageService.store(file, category);
        return new UploadResultDto(
                url, file.getOriginalFilename(), file.getContentType(), file.getSize());
    }
}
