import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import useApiResource from "../../hooks/useApiResource";
import Lightbox from "../../components/common/Lightbox";
import resolveMediaUrl from "../../utils/mediaUrl";

/**
 * One batch's album — every photo in it.
 *
 * KB §3.1: "clicking an album shows all its photos", with one designated
 * cover. The cover is shown first here rather than in its own slot: it's
 * the photo that represented the album on the way in, so leading with it
 * makes the transition feel continuous instead of dropping the visitor
 * into an unordered grid.
 */
export default function BatchAlbum() {
  const { batchId } = useParams();
  const { item: album, loading, error, fetchById } = useApiResource("/batches");
  const [lightbox, setLightbox] = useState(null);

  useEffect(() => {
    // The album lives at /batches/{id}/album, so the id carries the suffix.
    fetchById(`${batchId}/album`);
  }, [batchId]); // eslint-disable-line react-hooks/exhaustive-deps

  if (loading) return <p>Loading album...</p>;
  if (error) return <p className="error">{error}</p>;
  if (!album) return null;

  const photos = [...(album.images || [])].sort(
    (a, b) => Number(b.isCover) - Number(a.isCover)
  );

  return (
    <div className="album-page">
      <Link to="/campus-life">&larr; Back to Campus Life</Link>
      <h1>{album.title}</h1>
      <p className="album-page__meta">{album.batchName}</p>
      {album.description && <p>{album.description}</p>}

      {photos.length === 0 ? (
        <p>This album doesn&apos;t have any photos yet.</p>
      ) : (
        <div className="gallery-grid">
          {photos.map((img) => (
            <figure key={img.imageId} className="gallery-item">
              <button
                type="button"
                className="gallery-item__trigger"
                onClick={() => setLightbox(img)}
              >
                <img src={resolveMediaUrl(img.imageUrl)} alt={img.caption || album.batchName} />
              </button>
              {img.caption && <figcaption>{img.caption}</figcaption>}
            </figure>
          ))}
        </div>
      )}

      <Lightbox image={lightbox} onClose={() => setLightbox(null)} />
    </div>
  );
}
