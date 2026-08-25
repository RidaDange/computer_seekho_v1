import { useEffect, useState } from "react";
import axiosClient from "../../api/axiosClient";
import useApiResource from "../../hooks/useApiResource";

/**
 * Assigns a course's primary faculty.
 *
 * Kept out of the course edit form on purpose: is_primary lives on the
 * course_staff junction, not on Course, and has its own endpoint
 * (PUT /courses/{id}/primary-faculty/{staffId}) because the server has to
 * demote the previous holder in the same transaction. Folding it into the
 * course PUT would have meant the form silently owning a second table.
 */
export default function PrimaryFacultyDialog({ course, onClose, onSaved }) {
  const { data: faculty, fetchAll } = useApiResource("/staff/by-role/Faculty");

  const [staffId, setStaffId] = useState(course.primaryFacultyId ?? "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    fetchAll();
  }, [fetchAll]);

  const save = async () => {
    if (!staffId) {
      setError("Pick a faculty member.");
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await axiosClient.put(`/courses/${course.courseId}/primary-faculty/${staffId}`);
      onSaved();
      onClose();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="modal">
      <h3>Primary faculty — {course.name}</h3>
      <p className="modal__meta">
        Shown as the course's faculty on the public Course Detail page.
      </p>

      {error && <p className="error">{error}</p>}

      <label>
        Faculty
        <select value={staffId} onChange={(e) => setStaffId(e.target.value)}>
          <option value="">Select a faculty member...</option>
          {faculty.map((f) => (
            <option key={f.staffId} value={f.staffId}>
              {f.name}{f.qualification ? ` — ${f.qualification}` : ""}
            </option>
          ))}
        </select>
      </label>
      <p className="form-note">
        Only active staff with the Faculty role appear here. Assigning a new one automatically
        demotes the current primary; they stay on the course as a regular teacher.
      </p>

      <div className="modal__actions">
        <button type="button" onClick={save} disabled={saving}>
          {saving ? "Saving..." : "Set as primary"}
        </button>
        <button type="button" onClick={onClose}>Cancel</button>
      </div>
    </div>
  );
}
