package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.BatchAlbumSummaryDto;
import com.smvita.computerseekho.service.BatchAlbumService;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

/**
 * The public album index.
 *
 * Sits at its own top-level path rather than under /batches/{id}/album,
 * because it isn't scoped to a batch — it's "show me every album there is",
 * which the nested path can't express.
 */
@RestController
@RequestMapping("/batch-albums")
@RequiredArgsConstructor
public class BatchAlbumListController {

    private final BatchAlbumService batchAlbumService;

    @GetMapping
    public List<BatchAlbumSummaryDto> findPublished() {
        return batchAlbumService.findPublishedAlbums();
    }
}
