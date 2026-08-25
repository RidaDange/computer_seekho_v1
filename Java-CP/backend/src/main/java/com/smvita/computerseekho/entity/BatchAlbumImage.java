package com.smvita.computerseekho.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDate;

@Entity
@Table(name = "batch_album_images")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class BatchAlbumImage {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "image_id")
    private Integer imageId;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "album_id", nullable = false)
    private BatchAlbum album;

    @Column(name = "image_url", nullable = false, length = 500)
    private String imageUrl;

    @Column(name = "caption", length = 255)
    private String caption;

    /** Nullable FK — ON DELETE SET NULL, so removing a staff member doesn't take photos with them. */
    @Column(name = "uploaded_by")
    private Integer uploadedBy;

    @Column(name = "upload_date", nullable = false)
    private LocalDate uploadDate = LocalDate.now();

    @Column(name = "display_order", nullable = false)
    private Integer displayOrder = 0;

    @Column(name = "is_active", nullable = false)
    private Boolean isActive = true;
}
