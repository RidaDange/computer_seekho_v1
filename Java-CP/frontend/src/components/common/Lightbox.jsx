import { useEffect } from "react";
import resolveMediaUrl from "../../utils/mediaUrl";

/**
 * Click-to-enlarge overlay, shared by the batch album and the Campus Life
 * theme pages. Extracted the moment there was a second caller rather than
 * copied — the Escape handling and body-scroll lock are the kind of detail
 * that drifts out of sync between duplicates.
 */
export default function Lightbox({ image, onClose }) {
  useEffect(() => {
    if (!image) return undefined;

    const onKey = (e) => {
      if (e.key === "Escape") onClose();
    };
    document.addEventListener("keydown", onKey);

    // Stop the page behind scrolling while the overlay is up.
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    return () => {
      document.removeEventListener("keydown", onKey);
      document.body.style.overflow = previousOverflow;
    };
  }, [image, onClose]);

  if (!image) return null;

  return (
    <div className="lightbox" role="presentation" onClick={onClose}>
      <img src={resolveMediaUrl(image.imageUrl)} alt={image.caption || image.title || "Photo"} />
      {(image.caption || image.title) && <p>{image.caption || image.title}</p>}
      <button type="button" className="lightbox__close">Close</button>
    </div>
  );
}
