package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.BatchAlbumDto;
import com.smvita.computerseekho.dto.BatchAlbumImageDto;
import com.smvita.computerseekho.service.BatchAlbumService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.*;

/**
 * Nested under /batches because the album has no independent identity —
 * one per batch, and it lives inside the Batch Management screen (KB §9.5).
 */
@RestController
@RequestMapping("/batches/{batchId}/album")
@RequiredArgsConstructor
public class BatchAlbumController {

    private final BatchAlbumService batchAlbumService;

    @GetMapping
    public BatchAlbumDto find(@PathVariable Integer batchId) {
        return batchAlbumService.findByBatch(batchId);
    }

    @PostMapping("/images")
    public BatchAlbumDto addImage(@PathVariable Integer batchId,
                                  @Valid @RequestBody BatchAlbumImageDto image) {
        return batchAlbumService.addImage(batchId, image);
    }

    @PutMapping("/cover/{imageId}")
    public BatchAlbumDto setCover(@PathVariable Integer batchId, @PathVariable Integer imageId) {
        return batchAlbumService.setCover(batchId, imageId);
    }

    @DeleteMapping("/images/{imageId}")
    public BatchAlbumDto removeImage(@PathVariable Integer batchId, @PathVariable Integer imageId) {
        return batchAlbumService.removeImage(batchId, imageId);
    }

    @PutMapping
    public BatchAlbumDto updateDetails(@PathVariable Integer batchId,
                                       @RequestParam(required = false) String title,
                                       @RequestParam(required = false) String description) {
        return batchAlbumService.updateDetails(batchId, title, description);
    }
}
