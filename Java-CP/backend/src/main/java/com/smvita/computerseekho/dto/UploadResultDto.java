package com.smvita.computerseekho.dto;

/**
 * What the client needs after an upload: where the file now lives, and
 * enough about it to show a sensible confirmation.
 *
 * {@code url} is relative to the API root ("/uploads/students/....jpg"), not
 * absolute — the same value has to work from localhost and from a deployed
 * host, and only the client knows which one it's talking to.
 */
public record UploadResultDto(
        String url,
        String originalFilename,
        String contentType,
        long sizeBytes
) {}
