import TableMaintenanceGrid from "../../components/admin/TableMaintenanceGrid";

const columns = [
  { key: "categoryId", label: "ID", isId: true },
  { key: "name", label: "Name" },
  { key: "ageGroup", label: "Age Group" },
  { key: "isActive", label: "Active" },
];

function CourseCategoryForm(draft, setDraft) {
  return (
    <div className="form-fields">
      <label>
        Name
        <input value={draft.name || ""} onChange={(e) => setDraft({ ...draft, name: e.target.value })} />
      </label>
      <label>
        Age Group
        <input value={draft.ageGroup || ""} onChange={(e) => setDraft({ ...draft, ageGroup: e.target.value })} />
      </label>
      <label>
        Description
        <textarea
          value={draft.description || ""}
          onChange={(e) => setDraft({ ...draft, description: e.target.value })}
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

export default function CourseCategoriesMaintenance() {
  return (
    <TableMaintenanceGrid
      title="Course Categories"
      endpoint="/course-categories"
      columns={columns}
      renderForm={CourseCategoryForm}
    />
  );
}
