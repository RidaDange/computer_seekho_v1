package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.BatchAlbumDto;
import com.smvita.computerseekho.dto.BatchAlbumImageDto;
import com.smvita.computerseekho.dto.BatchAlbumSummaryDto;
import com.smvita.computerseekho.entity.Batch;
import com.smvita.computerseekho.entity.BatchAlbum;
import com.smvita.computerseekho.entity.BatchAlbumImage;
import com.smvita.computerseekho.exception.BusinessRuleException;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.BatchAlbumImageRepository;
import com.smvita.computerseekho.repository.BatchAlbumRepository;
import com.smvita.computerseekho.repository.BatchRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.Objects;

/**
 * The Batch Album — one per batch, several photos, one designated cover
 * (KB §4.1 / §9.5, and it lives inside the Batch Management screen rather
 * than as its own nav item).
 *
 * The album is created lazily on first access instead of being seeded
 * alongside every batch: most batches never get photos, and empty album
 * rows would just be noise that the UNIQUE(batch_id) constraint then makes
 * awkward to clean up.
 */
@Service
@RequiredArgsConstructor
@Transactional
public class BatchAlbumService {

    private final BatchAlbumRepository albumRepository;
    private final BatchAlbumImageRepository imageRepository;
    private final BatchRepository batchRepository;

    /**
     * The public Batch Albums strip.
     *
     * Albums with no photos are filtered out rather than shown as empty
     * cards: getOrCreateAlbum() creates the row the moment an admin opens
     * the Album tab, so an empty album means "someone looked", not
     * "there's something to see".
     */
    @Transactional(readOnly = true)
    public List<BatchAlbumSummaryDto> findPublishedAlbums() {
        return albumRepository.findPublished()
                .stream()
                .map(album -> {
                    List<BatchAlbumImage> images = activeImages(album);
                    if (images.isEmpty()) {
                        return null;
                    }
                    // Fall back to the first photo when no cover is set, so a
                    // half-configured album still renders something.
                    String coverUrl = album.getCoverImage() != null
                            ? album.getCoverImage().getImageUrl()
                            : images.get(0).getImageUrl();

                    return new BatchAlbumSummaryDto(
                            album.getAlbumId(),
                            album.getBatch().getBatchId(),
                            album.getBatch().getBatchName(),
                            album.getBatch().getCourse().getName(),
                            album.getBatch().getAcademicYear(),
                            album.getTitle(),
                            album.getDescription(),
                            coverUrl,
                            images.size());
                })
                .filter(java.util.Objects::nonNull)
                .toList();
    }

    @Transactional(readOnly = true)
    public BatchAlbumDto findByBatch(Integer batchId) {
        return albumRepository.findByBatch_BatchId(batchId)
                .map(this::toDto)
                .orElseGet(() -> emptyAlbumFor(batchId));
    }

    public BatchAlbumDto addImage(Integer batchId, BatchAlbumImageDto dto) {
        BatchAlbum album = getOrCreateAlbum(batchId);

        BatchAlbumImage image = new BatchAlbumImage();
        image.setAlbum(album);
        image.setImageUrl(dto.imageUrl().trim());
        image.setCaption(dto.caption());
        image.setDisplayOrder(dto.displayOrder() != null ? dto.displayOrder() : nextDisplayOrder(album));
        image.setIsActive(true);
        image = imageRepository.save(image);

        // First photo in becomes the cover automatically — an album with
        // photos but no cover would render blank on the public page, and
        // making the counsellor pick one manually just to see anything is
        // busywork.
        if (album.getCoverImage() == null) {
            album.setCoverImage(image);
            albumRepository.save(album);
        }

        return toDto(album);
    }

    public BatchAlbumDto setCover(Integer batchId, Integer imageId) {
        BatchAlbum album = albumRepository.findByBatch_BatchId(batchId)
                .orElseThrow(() -> new ResourceNotFoundException("This batch has no album yet"));

        BatchAlbumImage image = imageRepository.findById(imageId)
                .orElseThrow(() -> new ResourceNotFoundException("Album image", imageId));

        // The cover is picked *from* the album's own photos — a cover
        // belonging to a different batch's album is the exact drift the
        // cover_image_id FK was introduced to prevent (KB §4.1).
        if (!Objects.equals(image.getAlbum().getAlbumId(), album.getAlbumId())) {
            throw new BusinessRuleException("That photo belongs to a different batch's album.");
        }

        album.setCoverImage(image);
        return toDto(albumRepository.save(album));
    }

    public BatchAlbumDto removeImage(Integer batchId, Integer imageId) {
        BatchAlbum album = albumRepository.findByBatch_BatchId(batchId)
                .orElseThrow(() -> new ResourceNotFoundException("This batch has no album yet"));

        BatchAlbumImage image = imageRepository.findById(imageId)
                .orElseThrow(() -> new ResourceNotFoundException("Album image", imageId));

        // Clear the cover first if it's the one going — otherwise the FK
        // still points at the row being deactivated and the public page
        // shows a cover that's no longer in the album.
        if (album.getCoverImage() != null
                && Objects.equals(album.getCoverImage().getImageId(), imageId)) {
            album.setCoverImage(null);
            albumRepository.save(album);
        }

        image.setIsActive(false);
        imageRepository.save(image);

        // Promote the next remaining photo so the album doesn't silently
        // lose its cover.
        List<BatchAlbumImage> remaining = activeImages(album);
        if (album.getCoverImage() == null && !remaining.isEmpty()) {
            album.setCoverImage(remaining.get(0));
            albumRepository.save(album);
        }

        return toDto(album);
    }

    public BatchAlbumDto updateDetails(Integer batchId, String title, String description) {
        BatchAlbum album = getOrCreateAlbum(batchId);
        if (title != null && !title.isBlank()) {
            album.setTitle(title.trim());
        }
        album.setDescription(description);
        return toDto(albumRepository.save(album));
    }

    // ------------------------------------------------------------ helpers

    private BatchAlbum getOrCreateAlbum(Integer batchId) {
        return albumRepository.findByBatch_BatchId(batchId).orElseGet(() -> {
            Batch batch = batchRepository.findById(batchId)
                    .orElseThrow(() -> new ResourceNotFoundException("Batch", batchId));
            BatchAlbum album = new BatchAlbum();
            album.setBatch(batch);
            album.setTitle(batch.getBatchName() + " Photos");
            album.setIsActive(true);
            return albumRepository.save(album);
        });
    }

    private int nextDisplayOrder(BatchAlbum album) {
        return activeImages(album).stream()
                .mapToInt(BatchAlbumImage::getDisplayOrder)
                .max().orElse(0) + 1;
    }

    private List<BatchAlbumImage> activeImages(BatchAlbum album) {
        return imageRepository
                .findByAlbum_AlbumIdAndIsActiveTrueOrderByDisplayOrderAscImageIdAsc(album.getAlbumId());
    }

    private BatchAlbumDto emptyAlbumFor(Integer batchId) {
        Batch batch = batchRepository.findById(batchId)
                .orElseThrow(() -> new ResourceNotFoundException("Batch", batchId));
        return new BatchAlbumDto(null, batchId, batch.getBatchName(),
                batch.getBatchName() + " Photos", null, null, null, true, List.of());
    }

    private BatchAlbumDto toDto(BatchAlbum album) {
        List<BatchAlbumImage> images = activeImages(album);
        Integer coverId = album.getCoverImage() != null ? album.getCoverImage().getImageId() : null;

        return new BatchAlbumDto(
                album.getAlbumId(),
                album.getBatch().getBatchId(),
                album.getBatch().getBatchName(),
                album.getTitle(),
                album.getDescription(),
                coverId,
                album.getCoverImage() != null ? album.getCoverImage().getImageUrl() : null,
                album.getIsActive(),
                images.stream()
                        .map(i -> new BatchAlbumImageDto(i.getImageId(), i.getImageUrl(), i.getCaption(),
                                i.getUploadDate(), i.getDisplayOrder(), i.getIsActive(),
                                Objects.equals(i.getImageId(), coverId)))
                        .toList());
    }
}
