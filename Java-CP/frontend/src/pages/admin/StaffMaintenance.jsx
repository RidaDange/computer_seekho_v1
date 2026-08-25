import { useState } from "react";
import TableMaintenanceGrid from "../../components/admin/TableMaintenanceGrid";

const columns = [
  { key: "staffId", label: "ID", isId: true },
  { key: "name", label: "Name" },
  { key: "role", label: "Role" },
  { key: "username", label: "Username" },
  { key: "isActive", label: "Active" },
];

const ROLES = ["Admin", "Counselor", "Faculty", "Manager", "Receptionist"];

function StaffForm(draft, setDraft) {
  return (
    <div className="form-fields">
      <label>
        Name
        <input value={draft.name || ""} onChange={(e) => setDraft({ ...draft, name: e.target.value })} />
      </label>
      <label>
        Email
        <input value={draft.email || ""} onChange={(e) => setDraft({ ...draft, email: e.target.value })} />
      </label>
      <label>
        Phone
        <input value={draft.phone || ""} onChange={(e) => setDraft({ ...draft, phone: e.target.value })} />
      </label>
      <label>
        Role
        <select value={draft.role || ""} onChange={(e) => setDraft({ ...draft, role: e.target.value })}>
          <option value="">Select role...</option>
          {ROLES.map((r) => (
            <option key={r} value={r}>{r}</option>
          ))}
        </select>
      </label>
      <label>
        Username
        <input value={draft.username || ""} onChange={(e) => setDraft({ ...draft, username: e.target.value })} />
      </label>
      <label>
        Qualification
        <input
          value={draft.qualification || ""}
          onChange={(e) => setDraft({ ...draft, qualification: e.target.value })}
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
      {/* Deliberately no password field here — see StaffService.create on
          the backend. A temp password is generated server-side and shown
          once after saving, not typed in here. */}
    </div>
  );
}

export default function StaffMaintenance() {
  const [tempPasswordNotice, setTempPasswordNotice] = useState(null);

  const handleSaved = (response) => {
    // Only the CREATE response has this shape ({ staff, temporaryPassword });
    // updates return a plain StaffDto and won't match this check.
    if (response?.temporaryPassword) {
      setTempPasswordNotice(
        `Staff account created for "${response.staff.username}". ` +
          `One-time temporary password: ${response.temporaryPassword} ` +
          `— share this with them now; it will not be shown again.`
      );
    }
  };

  return (
    <div>
      {tempPasswordNotice && (
        <div className="notice notice--important">
          {tempPasswordNotice}
          <button onClick={() => setTempPasswordNotice(null)}>Dismiss</button>
        </div>
      )}
      <TableMaintenanceGrid
        title="Staff"
        endpoint="/staff"
        columns={columns}
        renderForm={StaffForm}
        onSaved={handleSaved}
      />
    </div>
  );
}
