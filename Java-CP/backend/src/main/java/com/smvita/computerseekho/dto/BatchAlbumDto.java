package com.smvita.computerseekho.dto;

import java.util.List;

public record BatchAlbumDto(
        Integer albumId,
        Integer batchId,
        String batchName,
        String title,
        String description,
        Integer coverImageId,
        String coverImageUrl,
        Boolean isActive,
        List<BatchAlbumImageDto> images
) {}
