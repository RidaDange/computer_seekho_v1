import { useEffect, useMemo, useState } from "react";
import ConfirmDialog from "../common/ConfirmDialog";
import useApiResource from "../../hooks/useApiResource";

/**
 * TableMaintenanceGrid — the single generic component behind BRD §6.4:
 * "This option will have a dropdown menu which will list out all master
 * tables... the list of all records in tabular format (grid) with the
 * facility of searching any particular record. Each row will have
 * Update/Delete... 'Add' button on top."
 *
 * One component, driven entirely by a `columns` config, is what lets every
 * master table (course_categories, courses, recruiters, announcements,
 * closure_reasons, banners, news_events, testimonials, gallery_images,
 * placement_drives, placement_records, staff) reuse the exact same grid
 * instead of a bespoke screen per table.
 *
 * `columns`: [{ key, label, editable = true }]
 * `endpoint`: e.g. "/recruiters"
 * `renderForm`: (draft, setDraft) => JSX — the add/edit form body; kept as
 *   an injected render prop since field types genuinely differ per table
 *   (e.g. recruiters needs a logo upload, announcements needs date pickers).
 * `rowActions`: (row, refresh) => JSX — optional extra buttons in the Action
 *   column. A few tables have operations that aren't plain CRUD and don't
 *   belong in the edit form: assigning a course's primary faculty writes to
 *   the course_staff junction, not to a Course field, so it gets its own
 *   endpoint and its own button rather than a fake column.
 */
export default function TableMaintenanceGrid({ title, endpoint, columns, renderForm, onSaved, rowActions }) {
  const { data, loading, error, fetchAll, create, update, remove } = useApiResource(endpoint);
  const [search, setSearch] = useState("");
  const [editingRow, setEditingRow] = useState(null); // null = closed, {} = new, {...row} = editing
  const [draft, setDraft] = useState({});
  const [confirmingDelete, setConfirmingDelete] = useState(null);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    fetchAll();
  }, [fetchAll]);

  const filteredData = useMemo(() => {
    if (!search.trim()) return data;
    const q = search.toLowerCase();
    return data.filter((row) =>
      columns.some((col) => String(row[col.key] ?? "").toLowerCase().includes(q))
    );
  }, [data, search, columns]);

  const openAdd = () => {
    setDraft({});
    setEditingRow({});
  };

  const openEdit = (row) => {
    setDraft(row);
    setEditingRow(row);
  };

  const closeForm = () => {
    setEditingRow(null);
    setDraft({});
  };

  const handleSave = async () => {
    const idKey = columns.find((c) => c.isId)?.key || "id";
    let response;
    if (editingRow?.[idKey]) {
      response = await update(editingRow[idKey], draft);
    } else {
      response = await create(draft);
    }
    onSaved?.(response);
    closeForm();
    fetchAll();
  };

  const handleDelete = async () => {
    const idKey = columns.find((c) => c.isId)?.key || "id";
    setDeleting(true);
    try {
      await remove(confirmingDelete[idKey]);
      fetchAll();
    } finally {
      setDeleting(false);
      setConfirmingDelete(null);
    }
  };

  return (
    <div className="table-maintenance">
      <div className="table-maintenance__toolbar">
        <h2>{title}</h2>
        <input
          type="text"
          placeholder={`Search ${title.toLowerCase()}...`}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <button onClick={openAdd}>+ Add</button>
      </div>

      {loading && <p>Loading...</p>}
      {error && <p className="error">{error}</p>}

      <table>
        <thead>
          <tr>
            {columns.map((col) => (
              <th key={col.key}>{col.label}</th>
            ))}
            <th>Action</th>
          </tr>
        </thead>
        <tbody>
          {filteredData.map((row, i) => (
            <tr key={row.id ?? i}>
              {columns.map((col) => (
                <td key={col.key}>{String(row[col.key] ?? "")}</td>
              ))}
              <td>
                <button onClick={() => openEdit(row)}>Update</button>
                <button onClick={() => setConfirmingDelete(row)}>Delete</button>
                {rowActions?.(row, fetchAll)}
              </td>
            </tr>
          ))}
          {!loading && filteredData.length === 0 && (
            <tr>
              <td colSpan={columns.length + 1}>No records found.</td>
            </tr>
          )}
        </tbody>
      </table>

      {editingRow !== null && (
        <div className="table-maintenance__modal">
          <h3>{editingRow?.id ? "Update" : "Add"} {title}</h3>
          {renderForm(draft, setDraft)}
          <div className="table-maintenance__modal-actions">
            <button onClick={handleSave}>Save</button>
            <button onClick={closeForm}>Cancel</button>
          </div>
        </div>
      )}

      <ConfirmDialog
        open={Boolean(confirmingDelete)}
        title={`Delete ${title.toLowerCase()} record`}
        message="This can't be undone."
        detail="If the record is referenced elsewhere the database will refuse the delete, and you'll see why."
        confirmLabel="Delete"
        busy={deleting}
        onConfirm={handleDelete}
        onCancel={() => setConfirmingDelete(null)}
      />
    </div>
  );
}
