import { useEffect } from "react";
import { Link } from "react-router-dom";
import useApiResource from "../../hooks/useApiResource";
import resolveMediaUrl from "../../utils/mediaUrl";

/**
 * Campus Life — themed photo collections, plus batch albums.
 *
 * Photos are grouped into clickable themes (Lab Sessions, Guest Lectures,
 * ...) rather than dumped into one flat grid. With a real gallery behind
 * it a single grid becomes an undifferentiated wall of images; a visitor
 * looking for "what does a lab session look like" has no way in.
 *
 * Batch albums stay a separate section: gallery images are curated campus
 * content maintained through Table Maintenance, while an album belongs to
 * one cohort. Merging them would lose the "which batch is this?" context.
 */
export default function CampusLife() {
  const themesApi = useApiResource("/gallery-images/categories");
  const albumsApi = useApiResource("/batch-albums");

  useEffect(() => {
    themesApi.fetchAll();
    albumsApi.fetchAll();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <div className="campus-life-page">
      <h1>Campus Life</h1>
      <p>A look at life at SMVITA — click a theme to see all its photos.</p>

      {themesApi.loading && <p>Loading gallery...</p>}
      {themesApi.error && <p className="error">{themesApi.error}</p>}

      <div className="course-grid">
        {themesApi.data.map((t) => (
          <Link
            key={t.category}
            to={`/campus-life/gallery/${encodeURIComponent(t.category)}`}
            className="album-card"
          >
            <img src={resolveMediaUrl(t.coverImageUrl)} alt={t.category} />
            <div className="album-card__body">
              <strong>{t.category}</strong>
              <span className="album-card__count">
                {t.photoCount} photo{t.photoCount === 1 ? "" : "s"}
              </span>
            </div>
          </Link>
        ))}
      </div>

      {!themesApi.loading && themesApi.data.length === 0 && (
        <p>No photos yet — add some via Table Maintenance.</p>
      )}

      {/* Albums with no photos never reach here — the API filters them out,
          so this section simply disappears rather than showing empty cards. */}
      {albumsApi.data.length > 0 && (
        <section className="album-strip">
          <h2>Batch Albums</h2>
          <p>Photos from each batch — click an album to see all its pictures.</p>

          <div className="course-grid">
            {albumsApi.data.map((a) => (
              <Link key={a.albumId} to={`/campus-life/albums/${a.batchId}`} className="album-card">
                <img src={resolveMediaUrl(a.coverImageUrl)} alt={a.title} />
                <div className="album-card__body">
                  <strong>{a.batchName}</strong>
                  <span>{a.courseName}{a.academicYear ? ` · ${a.academicYear}` : ""}</span>
                  <span className="album-card__count">
                    {a.photoCount} photo{a.photoCount === 1 ? "" : "s"}
                  </span>
                </div>
              </Link>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
