import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import axiosClient from "../../api/axiosClient";
import useApiResource from "../../hooks/useApiResource";
import EnquiryFields from "../../components/common/EnquiryFields";
import { validateEnquiry } from "../../utils/enquiryValidation";

const SOURCES = ["Walk-in", "Phone", "eMail", "Referral", "Website"];

/**
 * Staff-side enquiry entry — the visitor who turns up at the campus in
 * person. Same fields and same server-side rules as the public form; the
 * only differences are that the counselor can record how the lead arrived,
 * and that the confirmation email still goes to the enquirer, not to them.
 */
export default function AddEnquiry() {
  const navigate = useNavigate();
  const { data: courses, fetchAll } = useApiResource("/courses/active");

  const [form, setForm] = useState({ source: "Walk-in" });
  const [fieldErrors, setFieldErrors] = useState({});
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState(null);

  useEffect(() => {
    fetchAll();
  }, [fetchAll]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);

    const errors = validateEnquiry(form);
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) return;

    setSubmitting(true);
    try {
      const { data } = await axiosClient.post("/inquiries", {
        courseId: Number(form.courseId),
        enquirerName: form.enquirerName.trim(),
        email: form.email.trim(),
        phone: form.phone.trim(),
        message: form.message?.trim() || null,
        source: form.source || "Walk-in",
      });
      setResult(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  if (result) {
    return (
      <div className="add-enquiry-page">
        <h2>Enquiry #{result.inquiryId} saved</h2>
        <p className="notice">
          Assigned to <strong>{result.staffName || "nobody yet — no active counsellor"}</strong>.
          {result.nextFollowupDate && <> First follow-up scheduled for <strong>{result.nextFollowupDate}</strong>.</>}
          {" "}A confirmation email has been sent to {result.email}.
        </p>
        <div className="modal__actions">
          <button type="button" onClick={() => { setResult(null); setForm({ source: "Walk-in" }); }}>
            Add another
          </button>
          <button type="button" onClick={() => navigate("/admin/followups")}>
            Back to follow-ups
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="add-enquiry-page">
      <h2>Add Enquiry</h2>
      <p className="form-note">
        For visitors enquiring in person or by phone. The counsellor is assigned automatically and
        the first follow-up is scheduled three days out.
      </p>

      {error && <p className="error">{error}</p>}

      <form onSubmit={handleSubmit}>
        <EnquiryFields form={form} setForm={setForm} courses={courses} fieldErrors={fieldErrors} />

        <div className="form-fields">
          <label>
            How did they reach us?
            <select
              value={form.source || "Walk-in"}
              onChange={(e) => setForm({ ...form, source: e.target.value })}
            >
              {SOURCES.map((s) => (
                <option key={s} value={s}>{s}</option>
              ))}
            </select>
          </label>
        </div>

        <button type="submit" disabled={submitting}>
          {submitting ? "Saving..." : "Save enquiry"}
        </button>
      </form>
    </div>
  );
}
