import TableMaintenanceGrid from "../../components/admin/TableMaintenanceGrid";

const columns = [
  { key: "reasonId", label: "ID", isId: true },
  { key: "reasonText", label: "Reason" },
  { key: "isActive", label: "Active" },
];

function ClosureReasonForm(draft, setDraft) {
  return (
    <div className="form-fields">
      <label>
        Reason
        <input
          value={draft.reasonText || ""}
          onChange={(e) => setDraft({ ...draft, reasonText: e.target.value })}
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

export default function ClosureReasonsMaintenance() {
  return (
    <TableMaintenanceGrid
      title="Closure Reasons"
      endpoint="/closure-reasons"
      columns={columns}
      renderForm={ClosureReasonForm}
    />
  );
}
