package com.smvita.computerseekho.service;

import com.smvita.computerseekho.exception.BusinessRuleException;
import jakarta.annotation.PostConstruct;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.web.multipart.MultipartFile;

import javax.imageio.ImageIO;
import java.awt.image.BufferedImage;
import java.io.IOException;
import java.io.InputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.StandardCopyOption;
import java.util.Map;
import java.util.Set;
import java.util.UUID;

/**
 * Stores uploaded images on disk and hands back the path they're served from.
 *
 * Files live on the filesystem rather than in the database. Image bytes in a
 * BLOB column bloat every backup and every dump of a schema that's otherwise
 * small enough to hand around, and they'd be streamed through the application
 * on each request instead of being served straight off the static handler.
 * The database keeps the URL, which is what every entity already stored.
 *
 * Replaces the Day-4 arrangement where student photos and album images were
 * URLs typed into a text box — workable for seeded demo data, useless the
 * moment someone has an actual photo on their laptop.
 */
@Service
public class FileStorageService {

    private static final Logger log = LoggerFactory.getLogger(FileStorageService.class);

    /**
     * The extension is derived from the content type rather than taken from
     * the uploaded filename. A client-supplied name can carry a second
     * extension ("photo.jpg.html") or path separators, and neither survives
     * being discarded entirely.
     */
    private static final Map<String, String> ALLOWED_TYPES = Map.of(
            "image/jpeg", "jpg",
            "image/pjpeg", "jpg",
            "image/png", "png",
            "image/gif", "gif",
            "image/webp", "webp"
    );

    /**
     * Where an upload is allowed to land. A fixed set rather than a free
     * string: the category becomes a directory name, so accepting arbitrary
     * input here is how "../../" ends up in a path.
     */
    private static final Set<String> CATEGORIES = Set.of(
            "students", "staff", "batches", "courses", "gallery",
            "banners", "testimonials", "recruiters"
    );

    /** Public prefix these files are served under, matched by MediaConfig. */
    public static final String URL_PREFIX = "/uploads";

    private final Path root;

    public FileStorageService(@Value("${app.uploads.dir:./uploads}") String uploadsDir) {
        this.root = Paths.get(uploadsDir).toAbsolutePath().normalize();
    }

    @PostConstruct
    void createStorageDirectory() {
        try {
            Files.createDirectories(root);
            log.info("Uploads directory: {}", root);
        } catch (IOException ex) {
            throw new IllegalStateException("Could not create uploads directory at " + root, ex);
        }
    }

    /**
     * Validates and stores the file, returning the path it will be served
     * from — e.g. {@code /uploads/students/3f2a....jpg}.
     *
     * The path is stored relative to the API root rather than as a full URL
     * so the same database row works against localhost, a staging host and a
     * production domain without a rewrite.
     */
    public String store(MultipartFile file, String category) {
        if (file == null || file.isEmpty()) {
            throw new BusinessRuleException("No file was uploaded.");
        }
        if (!CATEGORIES.contains(category)) {
            throw new BusinessRuleException(
                    "'" + category + "' isn't a valid upload category. Expected one of " + CATEGORIES + ".");
        }

        String contentType = file.getContentType() == null
                ? "" : file.getContentType().toLowerCase();
        String extension = ALLOWED_TYPES.get(contentType);
        if (extension == null) {
            throw new BusinessRuleException(
                    "Only JPG, PNG, GIF and WebP images can be uploaded — that file is " +
                            (contentType.isBlank() ? "of an unknown type" : contentType) + ".");
        }

        // The declared content type is just a header the client chose, so it
        // proves nothing on its own. Decoding the bytes does: anything that
        // isn't really an image fails here, which is what stops a renamed
        // executable from being stored and later handed back to a browser.
        //
        // WebP has no ImageIO reader on a stock JDK, so it would fail this
        // check despite being a perfectly good upload. It's exempted rather
        // than dropped from the allowed list — the extension still comes from
        // the content type, so a spoofed one can only ever be served as an
        // image the browser then refuses to render.
        if (!"image/webp".equals(contentType) && !isDecodableImage(file)) {
            throw new BusinessRuleException(
                    "That file claims to be an image but couldn't be read as one.");
        }

        Path directory = root.resolve(category).normalize();
        // Belt and braces. CATEGORIES already makes traversal impossible;
        // this catches the case where someone later widens that set without
        // thinking about what a category is allowed to contain.
        if (!directory.startsWith(root)) {
            throw new BusinessRuleException("Invalid upload category.");
        }

        String filename = UUID.randomUUID() + "." + extension;
        try {
            Files.createDirectories(directory);
            try (InputStream in = file.getInputStream()) {
                Files.copy(in, directory.resolve(filename), StandardCopyOption.REPLACE_EXISTING);
            }
        } catch (IOException ex) {
            log.error("Failed to store upload in {}: {}", directory, ex.getMessage());
            throw new BusinessRuleException("The file couldn't be saved. Please try again.");
        }

        String url = URL_PREFIX + "/" + category + "/" + filename;
        log.info("Stored upload {} ({} bytes) as {}", file.getOriginalFilename(), file.getSize(), url);
        return url;
    }

    /**
     * Deletes a previously stored file, given the URL that {@link #store}
     * returned. Silently does nothing for anything else — the seeded records
     * point at pravatar and placehold.co, and an external URL is not ours to
     * delete or to fail over.
     */
    public void deleteByUrl(String url) {
        if (url == null || !url.startsWith(URL_PREFIX + "/")) {
            return;
        }
        Path target = root.resolve(url.substring(URL_PREFIX.length() + 1)).normalize();
        if (!target.startsWith(root)) {
            return;
        }
        try {
            Files.deleteIfExists(target);
        } catch (IOException ex) {
            // A file we can't remove is litter, not a failure the caller
            // needs to hear about — the record it belonged to is already gone.
            log.warn("Could not delete {}: {}", target, ex.getMessage());
        }
    }

    private boolean isDecodableImage(MultipartFile file) {
        try (InputStream in = file.getInputStream()) {
            BufferedImage image = ImageIO.read(in);
            return image != null;
        } catch (IOException ex) {
            return false;
        }
    }
}
