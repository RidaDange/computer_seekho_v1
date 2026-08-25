const API_BASE = import.meta.env.VITE_API_BASE_URL || "http://localhost:8080/api";

/**
 * Turns a stored image reference into something an <img> tag can load.
 *
 * Two kinds of value live in these columns and both have to keep working:
 *
 *   - Absolute URLs from the seed data (pravatar, placehold.co) and anything
 *     an admin pastes in by hand. Returned untouched.
 *   - Paths written by the upload endpoint ("/uploads/students/x.jpg"), which
 *     are relative to the API root and would otherwise resolve against the
 *     Vite dev server on :5173 rather than the backend on :8080.
 *
 * Storing absolute URLs at upload time would have avoided the second case,
 * but then every row would hard-code a hostname and moving environments
 * would mean rewriting the database.
 */
export default function resolveMediaUrl(url) {
  if (!url) return "";

  const value = String(url).trim();
  if (/^(https?:)?\/\//i.test(value) || value.startsWith("data:")) {
    return value;
  }

  return `${API_BASE.replace(/\/$/, "")}${value.startsWith("/") ? "" : "/"}${value}`;
}
