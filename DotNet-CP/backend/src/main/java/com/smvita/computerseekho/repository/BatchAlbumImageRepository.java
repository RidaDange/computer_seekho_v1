package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.BatchAlbumImage;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface BatchAlbumImageRepository extends JpaRepository<BatchAlbumImage, Integer> {
    List<BatchAlbumImage> findByAlbum_AlbumIdAndIsActiveTrueOrderByDisplayOrderAscImageIdAsc(Integer albumId);
}
