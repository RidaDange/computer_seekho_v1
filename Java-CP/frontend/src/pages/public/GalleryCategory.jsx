import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import useApiResource from "../../hooks/useApiResource";
import Lightbox from "../../components/common/Lightbox";
import resolveMediaUrl from "../../utils/mediaUrl";

/**
 * Every photo under one Campus Life theme — "Lab Sessions",
 * "Guest Lectures" and so on.
 *
 * The theme travels in the URL, so it's decoded on the way in and
 * re-encoded on the way out to the API; a name like "Placement Prep"
 * would otherwise break the request path.
 *
 * Reads `item` rather than `data` because this goes through the hook's
 * fetchById branch — the endpoint takes the theme where an id would
 * normally sit, and the hook stores whatever comes back without caring
 * that the payload happens to be a list.
 */
export default function GalleryCategory() {
  const { category } = useParams();
  const theme = decodeURIComponent(category);

  const { item, loading, error, fetchById } = useApiResource("/gallery-images/by-category");
  const [lightbox, setLightbox] = useState(null);

  useEffect(() => {
    fetchById(encodeURIComponent(theme));
  }, [theme]); // eslint-disable-line react-hooks/exhaustive-deps

  const images = item || [];

  return (
    <div className="gallery-category-page">
      <Link to="/campus-life">&larr; Back to Campus Life</Link>
      <h1>{theme}</h1>

      {loading && <p>Loading photos...</p>}
      {error && <p className="error">{error}</p>}

      <div className="gallery-grid">
        {images.map((img) => (
          <figure key={img.imageId} className="gallery-item">
            <button
              type="button"
              className="gallery-item__trigger"
              onClick={() => setLightbox(img)}
            >
              <img src={resolveMediaUrl(img.imageUrl)} alt={img.title} />
            </button>
            <figcaption>
              <strong>{img.title}</strong>
              {img.description && <p>{img.description}</p>}
            </figcaption>
          </figure>
        ))}
      </div>

      {!loading && images.length === 0 && <p>No photos under this theme yet.</p>}

      <Lightbox image={lightbox} onClose={() => setLightbox(null)} />
    </div>
  );
}
