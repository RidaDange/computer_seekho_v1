import { useEffect, useState } from "react";
import TableMaintenanceGrid from "../../components/admin/TableMaintenanceGrid";
import PrimaryFacultyDialog from "../../components/admin/PrimaryFacultyDialog";
import useApiResource from "../../hooks/useApiResource";

const columns = [
  { key: "courseId", label: "ID", isId: true },
  { key: "name", label: "Name" },
  { key: "categoryName", label: "Category" },
  { key: "fees", label: "Fees" },
  { key: "primaryFacultyName", label: "Primary Faculty" },
  { key: "isActive", label: "Active" },
];

function CourseForm(draft, setDraft, categories) {
  return (
    <div className="form-fields">
      <label>
        Name
        <input value={draft.name || ""} onChange={(e) => setDraft({ ...draft, name: e.target.value })} />
      </label>
      <label>
        Category
        <select
          value={draft.categoryId || ""}
          onChange={(e) => setDraft({ ...draft, categoryId: Number(e.target.value) })}
        >
          <option value="">Select category...</option>
          {categories.map((c) => (
            <option key={c.categoryId} value={c.categoryId}>{c.name}</option>
          ))}
        </select>
      </label>
      <label>
        Description
        <textarea
          value={draft.description || ""}
          onChange={(e) => setDraft({ ...draft, description: e.target.value })}
        />
      </label>
      <label>
        Duration
        <input value={draft.duration || ""} onChange={(e) => setDraft({ ...draft, duration: e.target.value })} />
      </label>
      <label>
        Fees (₹)
        <input
          type="number"
          value={draft.fees ?? ""}
          onChange={(e) => setDraft({ ...draft, fees: Number(e.target.value) })}
        />
      </label>
      <label>
        Level
        <select value={draft.level || "Beginner"} onChange={(e) => setDraft({ ...draft, level: e.target.value })}>
          <option value="Beginner">Beginner</option>
          <option value="Intermediate">Intermediate</option>
          <option value="Advanced">Advanced</option>
        </select>
      </label>
      <label>
        Syllabus URL
        <input
          value={draft.syllabusUrl || ""}
          onChange={(e) => setDraft({ ...draft, syllabusUrl: e.target.value })}
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
      <p className="form-note">
        Primary faculty isn't set here — use the <strong>Faculty</strong> button on the course's row.
        It writes to the course_staff junction rather than to the course itself, so it needs its own
        action.
      </p>
    </div>
  );
}

export default function CoursesMaintenance() {
  const categoriesApi = useApiResource("/course-categories");
  const [facultyDialogFor, setFacultyDialogFor] = useState(null);
  // The grid owns its own data, so it hands its refresh callback to
  // rowActions — that's how the Primary Faculty column updates after the
  // dialog saves, without lifting the whole list into this component.
  const [refreshGrid, setRefreshGrid] = useState(null);

  useEffect(() => {
    categoriesApi.fetchAll();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <>
      <TableMaintenanceGrid
        title="Courses"
        endpoint="/courses"
        columns={columns}
        renderForm={(draft, setDraft) => CourseForm(draft, setDraft, categoriesApi.data)}
        rowActions={(row, refresh) => (
          <button
            type="button"
            onClick={() => {
              setRefreshGrid(() => refresh);
              setFacultyDialogFor(row);
            }}
          >
            Faculty
          </button>
        )}
      />

      {facultyDialogFor && (
        <PrimaryFacultyDialog
          course={facultyDialogFor}
          onClose={() => setFacultyDialogFor(null)}
          onSaved={() => refreshGrid?.()}
        />
      )}
    </>
  );
}
