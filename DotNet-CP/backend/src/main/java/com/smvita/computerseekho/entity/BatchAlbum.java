package com.smvita.computerseekho.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDateTime;

/**
 * One photo album per batch — enforced by uk_batch_album UNIQUE(batch_id).
 *
 * cover_image_id references a row in batch_album_images rather than holding
 * a duplicate URL (KB §4.1, Turn 5 recommendation): the faculty note said
 * "any 1 photo will be the cover photo", so the cover is picked *from* the
 * album, and a copied URL would drift out of sync the moment the original
 * image is replaced.
 */
@Entity
@Table(name = "batch_albums")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class BatchAlbum {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "album_id")
    private Integer albumId;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "batch_id", nullable = false)
    private Batch batch;

    @Column(name = "title", nullable = false, length = 200)
    private String title = "Batch Photos";

    @Column(name = "description", columnDefinition = "TEXT")
    private String description;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "cover_image_id")
    private BatchAlbumImage coverImage;

    @Column(name = "created_at", nullable = false, updatable = false, insertable = false)
    private LocalDateTime createdAt;

    @Column(name = "is_active", nullable = false)
    private Boolean isActive = true;
}
