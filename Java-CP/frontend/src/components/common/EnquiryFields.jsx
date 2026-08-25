/**
 * The enquiry form's field set, shared by both entry channels — the public
 * website form and the staff Add Enquiry screen. Same fields, same
 * client-side hints, so the two channels can't drift apart; the actual
 * validation of record is the backend's InquiryCreateRequest.
 */
export default function EnquiryFields({
  form,
  setForm,
  courses,
  fieldErrors = {},
  // Fields the caller has already established and won't accept edits to —
  // the public form passes "email" once a visitor has signed in with Google,
  // because the server substitutes the verified address anyway and an
  // editable box would only invite someone to type one that gets discarded.
  readOnlyFields = [],
}) {
  const set = (key) => (e) => setForm({ ...form, [key]: e.target.value });
  const isLocked = (key) => readOnlyFields.includes(key);

  return (
    <div className="form-fields">
      <label>
        Course of interest *
        <select value={form.courseId || ""} onChange={set("courseId")}>
          <option value="">Select a course...</option>
          {courses.map((c) => (
            <option key={c.courseId} value={c.courseId}>{c.name}</option>
          ))}
        </select>
        {fieldErrors.courseId && <span className="field-error">{fieldErrors.courseId}</span>}
      </label>

      <label>
        Full name *
        <input value={form.enquirerName || ""} onChange={set("enquirerName")} maxLength={150} />
        {fieldErrors.enquirerName && <span className="field-error">{fieldErrors.enquirerName}</span>}
      </label>

      <label>
        Email *
        <input
          type="email"
          value={form.email || ""}
          onChange={set("email")}
          maxLength={150}
          readOnly={isLocked("email")}
          className={isLocked("email") ? "input--locked" : undefined}
        />
        {isLocked("email") && (
          <small className="form-note">Verified with Google — your enquiry is filed against this address.</small>
        )}
        {fieldErrors.email && <span className="field-error">{fieldErrors.email}</span>}
      </label>

      <label>
        Mobile number *
        <input
          type="tel"
          inputMode="numeric"
          value={form.phone || ""}
          onChange={(e) => setForm({ ...form, phone: e.target.value.replace(/\D/g, "").slice(0, 10) })}
          placeholder="10-digit mobile number"
        />
        {fieldErrors.phone && <span className="field-error">{fieldErrors.phone}</span>}
      </label>

      <label>
        Message
        <textarea value={form.message || ""} onChange={set("message")} maxLength={2000} />
      </label>
    </div>
  );
}
