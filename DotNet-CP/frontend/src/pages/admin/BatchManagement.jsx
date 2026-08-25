import { useEffect, useState } from "react";
import axiosClient from "../../api/axiosClient";
import ConfirmDialog from "../../components/common/ConfirmDialog";
import ImageUpload from "../../components/common/ImageUpload";
import useApiResource from "../../hooks/useApiResource";
import resolveMediaUrl from "../../utils/mediaUrl";

const STATUSES = ["Upcoming", "Ongoing", "Completed", "Cancelled"];

/**
 * Batch Management — a dedicated screen rather than another Table
 * Maintenance grid (KB §9.1). The reason is current_count and status:
 * they're system-driven, recomputed from enrollments, so a generic
 * add/edit grid would invite someone to hand-edit a number the server
 * immediately overwrites.
 *
 * The Batch Album lives here too (KB §9.5) as a tab on the selected batch,
 * not as its own nav entry.
 */
export default function BatchManagement() {
  const { data: batches, loading, error, fetchAll } = useApiResource("/batches/detailed");
  const coursesApi = useApiResource("/courses");
  const facultyApi = useApiResource("/staff/by-role/Faculty");

  const [editing, setEditing] = useState(null); // {} = new, {...} = edit
  const [draft, setDraft] = useState({});
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState(null);

  const [selected, setSelected] = useState(null); // batch whose tabs are open
  const [tab, setTab] = useState("roster");
  const [roster, setRoster] = useState([]);
  const [album, setAlbum] = useState(null);
  const [newImage, setNewImage] = useState({});
  const [panelError, setPanelError] = useState(null);
  const [confirmingDelete, setConfirmingDelete] = useState(null);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    fetchAll();
    coursesApi.fetchAll();
    facultyApi.fetchAll();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // ------------------------------------------------------------ batch CRUD

  const openAdd = () => {
    setDraft({ status: "Upcoming", capacity: 20, isActive: true });
    setEditing({});
    setFormError(null);
  };

  const openEdit = (b) => {
    setDraft({ ...b });
    setEditing(b);
    setFormError(null);
  };

  const save = async () => {
    setSaving(true);
    setFormError(null);
    const payload = {
      ...draft,
      courseId: Number(draft.courseId),
      staffId: Number(draft.staffId),
      capacity: Number(draft.capacity),
      startDate: draft.startDate || null,
      endDate: draft.endDate || null,
      presentationDate: draft.presentationDate || null,
    };
    try {
      if (editing?.batchId) {
        await axiosClient.put(`/batches/${editing.batchId}`, payload);
      } else {
        await axiosClient.post("/batches", payload);
      }
      setEditing(null);
      fetchAll();
    } catch (err) {
      setFormError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const remove = async () => {
    setPanelError(null);
    setDeleting(true);
    try {
      await axiosClient.delete(`/batches/${confirmingDelete.batchId}`);
      fetchAll();
    } catch (err) {
      setPanelError(err.message);
    } finally {
      setDeleting(false);
      setConfirmingDelete(null);
    }
  };

  // ------------------------------------------------------- roster & album

  const openPanel = async (b, which) => {
    setSelected(b);
    setTab(which);
    setPanelError(null);
    try {
      if (which === "roster") {
        const { data } = await axiosClient.get(`/students/by-batch/${b.batchId}`);
        setRoster(data);
      } else {
        const { data } = await axiosClient.get(`/batches/${b.batchId}/album`);
        setAlbum(data);
      }
    } catch (err) {
      setPanelError(err.message);
    }
  };

  const addImage = async () => {
    if (!newImage.imageUrl?.trim()) {
      setPanelError("An image URL is required.");
      return;
    }
    setPanelError(null);
    try {
      const { data } = await axiosClient.post(`/batches/${selected.batchId}/album/images`, {
        imageUrl: newImage.imageUrl.trim(),
        caption: newImage.caption || null,
      });
      setAlbum(data);
      setNewImage({});
    } catch (err) {
      setPanelError(err.message);
    }
  };

  const setCover = async (imageId) => {
    try {
      const { data } = await axiosClient.put(`/batches/${selected.batchId}/album/cover/${imageId}`);
      setAlbum(data);
    } catch (err) {
      setPanelError(err.message);
    }
  };

  const removeImage = async (imageId) => {
    try {
      const { data } = await axiosClient.delete(`/batches/${selected.batchId}/album/images/${imageId}`);
      setAlbum(data);
    } catch (err) {
      setPanelError(err.message);
    }
  };

  const set = (key) => (e) => setDraft({ ...draft, [key]: e.target.value });

  return (
    <div className="batch-page">
      <div className="page-header">
        <h2>Batch Management</h2>
        <button type="button" className="button" onClick={openAdd}>+ New Batch</button>
      </div>

      {loading && <p>Loading batches...</p>}
      {error && <p className="error">{error}</p>}
      {panelError && <p className="error">{panelError}</p>}

      <table>
        <thead>
          <tr>
            <th>Batch</th><th>Course</th><th>Faculty</th><th>Year</th>
            <th>Dates</th><th>Filled</th><th>Status</th><th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {batches.map((b) => (
            <tr key={b.batchId} className={selected?.batchId === b.batchId ? "row--highlight" : ""}>
              <td>{b.batchName}</td>
              <td>{b.courseName}</td>
              <td>{b.staffName}</td>
              <td>{b.academicYear || "—"}</td>
              <td>{b.startDate || "—"} → {b.endDate || "—"}</td>
              <td>
                {b.currentCount}/{b.capacity}
                {b.currentCount >= b.capacity && <span className="badge badge--overdue">Full</span>}
              </td>
              <td><span className="badge">{b.status}</span></td>
              <td className="cell--actions">
                <button type="button" onClick={() => openEdit(b)}>Edit</button>
                <button type="button" onClick={() => openPanel(b, "roster")}>Roster</button>
                <button type="button" onClick={() => openPanel(b, "album")}>Album</button>
                <button type="button" onClick={() => setConfirmingDelete(b)}>Delete</button>
              </td>
            </tr>
          ))}
          {!loading && batches.length === 0 && (
            <tr><td colSpan={8}>No batches yet.</td></tr>
          )}
        </tbody>
      </table>

      {selected && (
        <section className="batch-panel">
          <div className="batch-panel__tabs">
            <button
              type="button"
              className={tab === "roster" ? "is-active" : ""}
              onClick={() => openPanel(selected, "roster")}
            >
              Roster
            </button>
            <button
              type="button"
              className={tab === "album" ? "is-active" : ""}
              onClick={() => openPanel(selected, "album")}
            >
              Batch Album
            </button>
            <button type="button" className="link-button" onClick={() => setSelected(null)}>Close</button>
          </div>

          <h3>{selected.batchName}</h3>

          {tab === "roster" && (
            <table>
              <thead>
                <tr><th>#</th><th>Student</th><th>Contact</th><th>Registered</th></tr>
              </thead>
              <tbody>
                {roster.map((s) => (
                  <tr key={s.studentId}>
                    <td>{s.studentId}</td>
                    <td>{s.firstName} {s.lastName}</td>
                    <td>{s.phone}<br /><small>{s.email}</small></td>
                    <td>{s.regDate}</td>
                  </tr>
                ))}
                {roster.length === 0 && <tr><td colSpan={4}>Nobody enrolled yet.</td></tr>}
              </tbody>
            </table>
          )}

          {tab === "album" && album && (
            <div>
              <p className="form-note">
                One album per batch. The cover is chosen from the album's own photos, so it can't
                drift out of sync with what's actually in there.
              </p>

              <div className="album-add">
                <ImageUpload
                  label="New photo"
                  category="batches"
                  value={newImage.imageUrl || ""}
                  onChange={(url) => setNewImage({ ...newImage, imageUrl: url })}
                  hint="Upload the photo, add a caption, then Add photo."
                />
                <input
                  type="text"
                  placeholder="Caption (optional)"
                  value={newImage.caption || ""}
                  onChange={(e) => setNewImage({ ...newImage, caption: e.target.value })}
                />
                {/* Disabled until something has been uploaded — the server
                    requires an image URL, so the click would only ever be a
                    validation error. */}
                <button type="button" onClick={addImage} disabled={!newImage.imageUrl}>
                  Add photo
                </button>
              </div>

              <div className="album-grid">
                {album.images.map((img) => (
                  <figure key={img.imageId} className={img.isCover ? "is-cover" : ""}>
                    <img src={resolveMediaUrl(img.imageUrl)} alt={img.caption || "Batch photo"} />
                    <figcaption>
                      {img.caption || <em>No caption</em>}
                      <div>
                        {img.isCover ? (
                          <span className="badge badge--converted">Cover</span>
                        ) : (
                          <button type="button" className="link-button" onClick={() => setCover(img.imageId)}>
                            Make cover
                          </button>
                        )}
                        <button type="button" className="link-button" onClick={() => removeImage(img.imageId)}>
                          Remove
                        </button>
                      </div>
                    </figcaption>
                  </figure>
                ))}
                {album.images.length === 0 && <p>No photos in this album yet.</p>}
              </div>
            </div>
          )}
        </section>
      )}

      {editing && (
        <div className="modal">
          <h3>{editing.batchId ? "Edit" : "New"} batch</h3>
          {formError && <p className="error">{formError}</p>}

          <label>Batch name<input value={draft.batchName || ""} onChange={set("batchName")} /></label>
          <label>
            Course
            <select value={draft.courseId || ""} onChange={set("courseId")}>
              <option value="">Select...</option>
              {coursesApi.data.map((c) => (
                <option key={c.courseId} value={c.courseId}>{c.name}</option>
              ))}
            </select>
          </label>
          <label>
            Faculty
            <select value={draft.staffId || ""} onChange={set("staffId")}>
              <option value="">Select...</option>
              {facultyApi.data.map((f) => (
                <option key={f.staffId} value={f.staffId}>{f.name}</option>
              ))}
            </select>
          </label>
          <label>Academic year<input placeholder="2026-27" value={draft.academicYear || ""} onChange={set("academicYear")} /></label>
          <label>Start date<input type="date" value={draft.startDate || ""} onChange={set("startDate")} /></label>
          <label>End date<input type="date" value={draft.endDate || ""} onChange={set("endDate")} /></label>
          <label>Timing<input placeholder="9 AM - 5 PM" value={draft.timing || ""} onChange={set("timing")} /></label>
          <label>Capacity<input type="number" value={draft.capacity ?? ""} onChange={set("capacity")} /></label>
          <label>
            Status
            <select value={draft.status || "Upcoming"} onChange={set("status")}>
              {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
            </select>
          </label>
          <p className="form-note">
            Enrolled count isn't editable — it's recalculated from the roster on every registration.
          </p>

          <div className="modal__actions">
            <button type="button" onClick={save} disabled={saving}>
              {saving ? "Saving..." : "Save"}
            </button>
            <button type="button" onClick={() => setEditing(null)}>Cancel</button>
          </div>
        </div>
      )}

      <ConfirmDialog
        open={Boolean(confirmingDelete)}
        title={`Delete batch "${confirmingDelete?.batchName ?? ""}"`}
        message="This can't be undone."
        detail="A batch with students enrolled will be refused by the database — drop or move them first."
        confirmLabel="Delete batch"
        busy={deleting}
        onConfirm={remove}
        onCancel={() => setConfirmingDelete(null)}
      />
    </div>
  );
}
