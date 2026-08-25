import TableMaintenanceGrid from "../../components/admin/TableMaintenanceGrid";

const columns = [
  { key: "bannerId", label: "ID", isId: true },
  { key: "title", label: "Title" },
  { key: "imageUrl", label: "Image URL" },
  { key: "displayOrder", label: "Order" },
  { key: "isActive", label: "Active" },
];

function BannerForm(draft, setDraft) {
  return (
    <div className="form-fields">
      <label>
        Title
        <input value={draft.title || ""} onChange={(e) => setDraft({ ...draft, title: e.target.value })} />
      </label>
      <label>
        Image URL
        <input value={draft.imageUrl || ""} onChange={(e) => setDraft({ ...draft, imageUrl: e.target.value })} />
      </label>
      <label>
        Link URL
        <input value={draft.linkUrl || ""} onChange={(e) => setDraft({ ...draft, linkUrl: e.target.value })} />
      </label>
      <label>
        Display Order
        <input
          type="number"
          value={draft.displayOrder ?? 0}
          onChange={(e) => setDraft({ ...draft, displayOrder: Number(e.target.value) })}
        />
      </label>
      <label>
        Start Date
        <input
          type="date"
          value={draft.startDate || ""}
          onChange={(e) => setDraft({ ...draft, startDate: e.target.value })}
        />
      </label>
      <label>
        End Date
        <input
          type="date"
          value={draft.endDate || ""}
          onChange={(e) => setDraft({ ...draft, endDate: e.target.value })}
        />
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

export default function BannersMaintenance() {
  return (
    <TableMaintenanceGrid title="Banners" endpoint="/banners" columns={columns} renderForm={BannerForm} />
  );
}
