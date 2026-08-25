import TableMaintenanceGrid from "../../components/admin/TableMaintenanceGrid";

const columns = [
  { key: "newsId", label: "ID", isId: true },
  { key: "title", label: "Title" },
  { key: "eventDate", label: "Event Date" },
  { key: "isActive", label: "Active" },
];

function NewsEventForm(draft, setDraft) {
  return (
    <div className="form-fields">
      <label>
        Title
        <input value={draft.title || ""} onChange={(e) => setDraft({ ...draft, title: e.target.value })} />
      </label>
      <label>
        Content
        <textarea value={draft.content || ""} onChange={(e) => setDraft({ ...draft, content: e.target.value })} />
      </label>
      <label>
        Image URL
        <input value={draft.imageUrl || ""} onChange={(e) => setDraft({ ...draft, imageUrl: e.target.value })} />
      </label>
      <label>
        Event Date
        <input
          type="date"
          value={draft.eventDate || ""}
          onChange={(e) => setDraft({ ...draft, eventDate: e.target.value })}
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

export default function NewsEventsMaintenance() {
  return (
    <TableMaintenanceGrid
      title="News & Events"
      endpoint="/news-events"
      columns={columns}
      renderForm={NewsEventForm}
    />
  );
}
