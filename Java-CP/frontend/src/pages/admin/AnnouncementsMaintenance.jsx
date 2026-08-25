import TableMaintenanceGrid from "../../components/admin/TableMaintenanceGrid";

const columns = [
  { key: "announcementId", label: "ID", isId: true },
  { key: "content", label: "Ticker Text" },
  { key: "startDate", label: "Start Date" },
  { key: "endDate", label: "End Date" },
  { key: "isActive", label: "Active" },
];

function AnnouncementForm(draft, setDraft) {
  return (
    <div className="form-fields">
      <label>
        Ticker Text
        <textarea
          maxLength={500}
          value={draft.content || ""}
          onChange={(e) => setDraft({ ...draft, content: e.target.value })}
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

export default function AnnouncementsMaintenance() {
  return (
    <TableMaintenanceGrid
      title="Announcements"
      endpoint="/announcements"
      columns={columns}
      renderForm={AnnouncementForm}
    />
  );
}
