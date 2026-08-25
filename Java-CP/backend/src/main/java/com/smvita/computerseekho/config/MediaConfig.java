package com.smvita.computerseekho.config;

import com.smvita.computerseekho.service.FileStorageService;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.servlet.config.annotation.ResourceHandlerRegistry;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;

import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * Serves uploaded images straight off disk.
 *
 * Kept apart from WebConfig, which explains at length why CORS is published
 * as a bean instead of through WebMvcConfigurer. That reasoning is specific
 * to CORS and the security filter chain; static resource handling has no such
 * problem, and folding it into that class would make the comment there read
 * as though it had been ignored.
 *
 * Because the application sits on a /api context path, a stored URL of
 * "/uploads/students/x.jpg" is reachable at "/api/uploads/students/x.jpg".
 * The client resolves it against the API base URL it already holds.
 */
@Configuration
public class MediaConfig implements WebMvcConfigurer {

    private final Path root;

    public MediaConfig(@Value("${app.uploads.dir:./uploads}") String uploadsDir) {
        this.root = Paths.get(uploadsDir).toAbsolutePath().normalize();
    }

    @Override
    public void addResourceHandlers(ResourceHandlerRegistry registry) {
        // toUri() rather than "file:" + path, so a directory containing a
        // space — "D:\CDAC PROJECT\..." — comes out percent-encoded instead
        // of producing a malformed location.
        String location = root.toUri().toString();
        // The trailing separator matters: without it Spring treats the
        // location as a file rather than a directory and every lookup misses.
        // Path.toUri() only adds one for a directory that already exists, and
        // whether FileStorageService has created it by now depends on bean
        // ordering, so it's appended here rather than assumed.
        if (!location.endsWith("/")) {
            location += "/";
        }

        registry.addResourceHandler(FileStorageService.URL_PREFIX + "/**")
                .addResourceLocations(location)
                // Uploaded files are immutable — the name is a fresh UUID on
                // every upload, so a cached copy can never be the wrong one.
                .setCachePeriod(31_536_000);
    }
}
