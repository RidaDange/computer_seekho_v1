import { useRef, useState } from "react";
import axiosClient from "../../api/axiosClient";
import resolveMediaUrl from "../../utils/mediaUrl";

const MAX_BYTES = 10 * 1024 * 1024; // matches spring.servlet.multipart.max-file-size

/**
 * Picks an image, uploads it, and hands the stored URL back to the parent.
 *
 * The upload happens immediately on selection rather than being deferred to
 * the surrounding form's submit. The server needs a real multipart request
 * and the form posts JSON, so bundling them would mean either a multipart
 * variant of every create endpoint or base64 in the payload. Uploading first
 * also means a validation failure elsewhere in the form doesn't cost the
 * user their photo.
 *
 * The parent keeps owning the value — this component never holds the URL as
 * state, it just reports it upward — so an existing record's photo renders
 * the same way whether it was uploaded today or seeded last year.
 */
export default function ImageUpload({
  value,
  onChange,
  category = "gallery",
  label = "Photo",
  hint,
}) {
  const inputRef = useRef(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState(null);

  const pick = async (event) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setError(null);

    // Checked here as well as on the server. The server's limit is the one
    // that counts, but a 30 MB file rejected locally saves the user watching
    // a long upload fail at the end of it.
    if (file.size > MAX_BYTES) {
      setError(`That file is ${(file.size / 1024 / 1024).toFixed(1)} MB — the limit is 10 MB.`);
      event.target.value = "";
      return;
    }

    const form = new FormData();
    form.append("file", file);
    form.append("category", category);

    setUploading(true);
    try {
      // Content-Type is deleted rather than set: the browser has to generate
      // it so it can append the multipart boundary. Leaving the client's
      // default application/json in place produces a request the server
      // cannot parse.
      const { data } = await axiosClient.post("/uploads", form, {
        headers: { "Content-Type": undefined },
      });
      onChange(data.url);
    } catch (err) {
      setError(err.message);
    } finally {
      setUploading(false);
      // Cleared so re-picking the same file after an error still fires
      // onChange — without this the input reports no change.
      event.target.value = "";
    }
  };

  return (
    <div className="image-upload">
      <span className="image-upload__label">{label}</span>

      {value ? (
        <div className="image-upload__preview">
          <img src={resolveMediaUrl(value)} alt="" />
          <button type="button" onClick={() => onChange("")} disabled={uploading}>
            Remove
          </button>
        </div>
      ) : (
        <p className="image-upload__empty">No image yet.</p>
      )}

      {/* Hidden and driven by the button below, so the control looks like the
          rest of the admin UI instead of a raw browser file input. */}
      <input
        ref={inputRef}
        type="file"
        accept="image/jpeg,image/png,image/gif,image/webp"
        onChange={pick}
        disabled={uploading}
        style={{ display: "none" }}
      />

      <button type="button" onClick={() => inputRef.current?.click()} disabled={uploading}>
        {uploading ? "Uploading..." : value ? "Replace image" : "Choose image"}
      </button>

      {hint && <small className="form-note">{hint}</small>}
      {error && <span className="field-error">{error}</span>}
    </div>
  );
}
