import TableMaintenanceGrid from "../../components/admin/TableMaintenanceGrid";

const columns = [
  { key: "testimonialId", label: "ID", isId: true },
  { key: "name", label: "Name" },
  { key: "rating", label: "Rating" },
  { key: "isApproved", label: "Approved" },
];

function TestimonialForm(draft, setDraft) {
  return (
    <div className="form-fields">
      <label>
        Name
        <input value={draft.name || ""} onChange={(e) => setDraft({ ...draft, name: e.target.value })} />
      </label>
      <label>
        Content
        <textarea value={draft.content || ""} onChange={(e) => setDraft({ ...draft, content: e.target.value })} />
      </label>
      <label>
        Rating (1-5)
        <input
          type="number"
          min={1}
          max={5}
          value={draft.rating ?? ""}
          onChange={(e) => setDraft({ ...draft, rating: Number(e.target.value) })}
        />
      </label>
      <label>
        Photo URL
        <input value={draft.photoUrl || ""} onChange={(e) => setDraft({ ...draft, photoUrl: e.target.value })} />
      </label>
      <label>
        <input
          type="checkbox"
          checked={Boolean(draft.isApproved)}
          onChange={(e) => setDraft({ ...draft, isApproved: e.target.checked })}
        />
        Approved (shown on public site)
      </label>
    </div>
  );
}

export default function TestimonialsMaintenance() {
  return (
    <TableMaintenanceGrid
      title="Testimonials"
      endpoint="/testimonials"
      columns={columns}
      renderForm={TestimonialForm}
    />
  );
}
