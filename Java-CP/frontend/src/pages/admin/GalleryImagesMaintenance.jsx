import TableMaintenanceGrid from "../../components/admin/TableMaintenanceGrid";

const columns = [
  { key: "imageId", label: "ID", isId: true },
  { key: "title", label: "Title" },
  { key: "category", label: "Category" },
  { key: "isActive", label: "Active" },
];

function GalleryImageForm(draft, setDraft) {
  return (
    <div className="form-fields">
      <label>
        Title
        <input value={draft.title || ""} onChange={(e) => setDraft({ ...draft, title: e.target.value })} />
      </label>
      <label>
        Description
        <textarea
          value={draft.description || ""}
          onChange={(e) => setDraft({ ...draft, description: e.target.value })}
        />
      </label>
      <label>
        Image URL
        <input value={draft.imageUrl || ""} onChange={(e) => setDraft({ ...draft, imageUrl: e.target.value })} />
      </label>
      <label>
        Category
        <input value={draft.category || ""} onChange={(e) => setDraft({ ...draft, category: e.target.value })} />
      </label>
      <label>
        <input
          type="checkbox"
          checked={Boolean(draft.isActive)}
          onChange={(e) => setDraft({ ...draft, isActive: e.target.checked })}
        />
        Active
      </label>
    </div>
  );
}

export default function GalleryImagesMaintenance() {
  return (
    <TableMaintenanceGrid
      title="Gallery Images"
      endpoint="/gallery-images"
      columns={columns}
      renderForm={GalleryImageForm}
    />
  );
}
