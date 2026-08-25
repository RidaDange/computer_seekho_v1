import TableMaintenanceGrid from "../../components/admin/TableMaintenanceGrid";

const columns = [
  { key: "recruiterId", label: "ID", isId: true },
  { key: "companyName", label: "Company Name" },
  { key: "logoUrl", label: "Logo URL" },
  { key: "isActive", label: "Active" },
];

function RecruiterForm(draft, setDraft) {
  return (
    <div className="form-fields">
      <label>
        Company Name
        <input
          value={draft.companyName || ""}
          onChange={(e) => setDraft({ ...draft, companyName: e.target.value })}
        />
      </label>
      <label>
        Logo URL
        <input
          value={draft.logoUrl || ""}
          onChange={(e) => setDraft({ ...draft, logoUrl: e.target.value })}
        />
      </label>
      <label>
        <input
          type="checkbox"
          checked={Boolean(draft.isActive)}
          onChange={(e) => setDraft({ ...draft, isActive: e.target.checked })}
        />
        Active (shown on public Our Recruiters page)
      </label>
    </div>
  );
}

export default function RecruitersMaintenance() {
  return (
    <TableMaintenanceGrid
      title="Recruiters"
      endpoint="/recruiters"
      columns={columns}
      renderForm={RecruiterForm}
    />
  );
}
