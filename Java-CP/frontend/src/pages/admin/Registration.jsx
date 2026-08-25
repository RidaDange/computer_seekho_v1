import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import axiosClient from "../../api/axiosClient";
import ImageUpload from "../../components/common/ImageUpload";
import useApiResource from "../../hooks/useApiResource";
import openReceipt from "../../utils/openReceipt";
import { validateStudentDetails } from "../../utils/studentValidation";

const PAYMENT_MODES = ["Cash", "UPI", "Card", "Bank Transfer", "Cheque"];
const GENDERS = ["Male", "Female", "Other"];

const money = (v) =>
  v == null ? "—" : `₹${Number(v).toLocaleString("en-IN", { minimumFractionDigits: 2 })}`;

/**
 * Student Registration — three steps, one transaction.
 *
 * Step 1 search enquiry · Step 2 student details · Step 3 batch + payment.
 * Nothing is written until the final submit: a student with no enrolment,
 * or an enrolment nobody paid for, is a half-registered record that no
 * later screen can use and nothing would ever clean up.
 *
 * "No enquiry, no registration" (KB §4.1) is why step 1 exists at all — you
 * cannot reach this form without picking an enquiry first.
 */
export default function Registration() {
  const navigate = useNavigate();
  const [params] = useSearchParams();

  const [step, setStep] = useState(1);
  const [query, setQuery] = useState("");
  const [inquiry, setInquiry] = useState(null);
  const [details, setDetails] = useState({});
  const [courseId, setCourseId] = useState("");
  const [batchId, setBatchId] = useState("");
  const [payment, setPayment] = useState({ paymentMode: "Cash" });
  const [fees, setFees] = useState(null);
  const [courses, setCourses] = useState([]);
  const [batches, setBatches] = useState([]);
  const [loadingCourse, setLoadingCourse] = useState(false);
  const [error, setError] = useState(null);
  const [fieldErrors, setFieldErrors] = useState({});
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState(null);

  const inquiriesApi = useApiResource("/registrations/eligible-inquiries");

  useEffect(() => {
    inquiriesApi.fetchAll({ q: query });
  }, [query]); // eslint-disable-line react-hooks/exhaustive-deps

  // Only active courses are offered. An inactive course is one the academy
  // has stopped selling, and the server refuses a registration against one
  // anyway — listing it would just be offering a click that always fails.
  useEffect(() => {
    axiosClient
      .get("/courses/active")
      .then(({ data }) => setCourses(data))
      .catch(() => {});
  }, []);

  // Deep link from the enquiry list: /admin/registration?inquiry=12
  useEffect(() => {
    const preset = params.get("inquiry");
    if (preset && !inquiry) {
      axiosClient.get(`/inquiries/${preset}`).then(({ data }) => selectInquiry(data)).catch(() => {});
    }
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  /**
   * Loads everything that depends on the course: the joinable batches and
   * the fee split. Both are fetched together because they must agree — a
   * batch list from one course beside a fee panel from another is exactly
   * the mismatch the server would reject on submit.
   */
  const loadCourse = async (id) => {
    setLoadingCourse(true);
    try {
      const [feeRes, batchRes] = await Promise.all([
        axiosClient.get(`/registrations/courses/${id}/fees`),
        axiosClient.get(`/registrations/courses/${id}/batches`),
      ]);
      setFees(feeRes.data);
      setBatches(batchRes.data);
      // Re-default the amount to the new installment 1. Carrying the old
      // course's figure over would be rejected as a short payment, or worse,
      // quietly overcharge on a cheaper course.
      setPayment((p) => ({ ...p, amountPaid: feeRes.data.installment1Amount }));
      return true;
    } catch (err) {
      setError(err.message);
      setFees(null);
      setBatches([]);
      return false;
    } finally {
      setLoadingCourse(false);
    }
  };

  const selectInquiry = async (i) => {
    setInquiry(i);
    setError(null);

    // Pre-fill from the enquiry so the counsellor isn't retyping a name and
    // phone number the prospect already gave once.
    const [first, ...rest] = (i.enquirerName || "").trim().split(/\s+/);
    setDetails((d) => ({
      ...d,
      firstName: d.firstName || first || "",
      lastName: d.lastName || rest.join(" "),
      email: d.email || i.email || "",
      phone: d.phone || i.phone || "",
    }));

    setCourseId(String(i.courseId ?? ""));
    setBatchId("");
    if (i.courseId) await loadCourse(i.courseId);
    setStep(2);
  };

  /**
   * Changing the course invalidates the batch already picked — it belongs to
   * the old course and the server would refuse it. Clearing it forces a
   * deliberate re-pick rather than letting a stale id ride along to submit.
   */
  const changeCourse = async (id) => {
    setCourseId(id);
    setBatchId("");
    setError(null);
    if (id) await loadCourse(id);
    else {
      setFees(null);
      setBatches([]);
    }
  };

  const submit = async () => {
    setSubmitting(true);
    setError(null);
    try {
      const { data } = await axiosClient.post("/registrations", {
        inquiryId: inquiry.inquiryId,
        courseId: courseId ? Number(courseId) : null,
        batchId: Number(batchId),
        student: {
          ...details,
          dob: details.dob || null,
          gender: details.gender || null,
        },
        amountPaid: payment.amountPaid,
        paymentMode: payment.paymentMode,
        transactionId: payment.transactionId || null,
        remarks: payment.remarks || null,
      });
      setResult(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  const set = (key) => (e) => setDetails({ ...details, [key]: e.target.value });

  // Marks a field with its message underneath, so the counsellor sees which
  // input to fix rather than a summary at the top of a 16-field form.
  const fieldError = (key) =>
    fieldErrors[key] ? <span className="field-error">{fieldErrors[key]}</span> : null;

  const continueToPayment = () => {
    const errors = validateStudentDetails(details);
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) {
      setError("Some details need fixing before you can continue.");
      return;
    }
    setError(null);
    setStep(3);
  };

  // ---------------------------------------------------------- confirmation

  if (result) {
    return (
      <div className="registration-page">
        <h2>Registered — student #{result.student.studentId}</h2>
        <p className="notice">
          {result.student.firstName} {result.student.lastName} is enrolled in{" "}
          <strong>{result.student.batchName}</strong>. Receipt{" "}
          <strong>{result.firstPayment.receiptNo}</strong> for {money(result.firstPayment.amount)}.
          {result.receiptEmailed
            ? ` A copy has been emailed to ${result.student.email}.`
            : " Email isn't configured, so hand over the printed receipt."}
        </p>

        <p>
          Installment 2 of <strong>{money(result.feeBreakdown.installment2Amount)}</strong> is due on{" "}
          <strong>{result.feeBreakdown.installment2DueDate}</strong>.
        </p>

        <div className="modal__actions">
          <button
            type="button"
            className="button"
            onClick={() => openReceipt(result.firstPayment.paymentId).catch((err) => setError(err.message))}
          >
            Open receipt (PDF)
          </button>
          <button type="button" onClick={() => navigate("/admin/students")}>Go to Students</button>
          <button
            type="button"
            onClick={() => {
              setResult(null); setStep(1); setInquiry(null); setDetails({});
              setCourseId(""); setBatchId(""); setPayment({ paymentMode: "Cash" });
              setFees(null); setBatches([]);
            }}
          >
            Register another
          </button>
        </div>
      </div>
    );
  }

  // ----------------------------------------------------------------- wizard

  return (
    <div className="registration-page">
      <h2>Student Registration</h2>

      <ol className="wizard-steps">
        {["Find enquiry", "Student details", "Batch & payment"].map((label, i) => (
          <li key={label} className={step === i + 1 ? "is-current" : step > i + 1 ? "is-done" : ""}>
            {i + 1}. {label}
          </li>
        ))}
      </ol>

      {error && <p className="error">{error}</p>}

      {step === 1 && (
        <section>
          <p className="form-note">
            A registration must reference an enquiry. Enquiries that are closed, or already
            registered, aren't listed.
          </p>
          <input
            type="text"
            className="search-input"
            placeholder="Search by name, phone, email or enquiry number..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            autoFocus
          />

          <table>
            <thead>
              <tr>
                <th>#</th><th>Enquirer</th><th>Contact</th><th>Course</th><th>Status</th><th></th>
              </tr>
            </thead>
            <tbody>
              {inquiriesApi.data.map((i) => (
                <tr key={i.inquiryId}>
                  <td>{i.inquiryId}</td>
                  <td>{i.enquirerName}</td>
                  <td>{i.phone}<br /><small>{i.email}</small></td>
                  <td>{i.courseName}</td>
                  <td><span className="badge">{i.status}</span></td>
                  <td>
                    <button type="button" onClick={() => selectInquiry(i)}>Select</button>
                  </td>
                </tr>
              ))}
              {!inquiriesApi.loading && inquiriesApi.data.length === 0 && (
                <tr><td colSpan={6}>No registerable enquiries match.</td></tr>
              )}
            </tbody>
          </table>
        </section>
      )}

      {step === 2 && (
        <section>
          <p className="notice">
            Registering <strong>{inquiry.enquirerName}</strong> (enquiry #{inquiry.inquiryId}) for{" "}
            <strong>{inquiry.courseName}</strong>.
          </p>

          <div className="form-grid">
            <label>First name *<input value={details.firstName || ""} onChange={set("firstName")} />{fieldError("firstName")}</label>
            <label>Last name *<input value={details.lastName || ""} onChange={set("lastName")} />{fieldError("lastName")}</label>
            <label>Parent / guardian *<input value={details.parentName || ""} onChange={set("parentName")} />{fieldError("parentName")}</label>
            <label>Parent phone<input value={details.parentPhone || ""} onChange={set("parentPhone")} />{fieldError("parentPhone")}</label>
            <label>Email *<input type="email" value={details.email || ""} onChange={set("email")} />{fieldError("email")}</label>
            <label>Phone *<input value={details.phone || ""} onChange={set("phone")} />{fieldError("phone")}</label>
            <label>Date of birth<input type="date" value={details.dob || ""} onChange={set("dob")} />{fieldError("dob")}</label>
            <label>
              Gender
              <select value={details.gender || ""} onChange={set("gender")}>
                <option value="">Not specified</option>
                {GENDERS.map((g) => <option key={g} value={g}>{g}</option>)}
              </select>
            </label>
            <label>Qualification<input value={details.qualification || ""} onChange={set("qualification")} /></label>
            <div className="form-grid__wide">
              <ImageUpload
                label="Student photo"
                category="students"
                value={details.photoUrl || ""}
                onChange={(url) => setDetails((d) => ({ ...d, photoUrl: url }))}
                hint="Optional. JPG, PNG, GIF or WebP, up to 10 MB."
              />
            </div>
            <label className="form-grid__wide">Address line 1<input value={details.addressLine1 || ""} onChange={set("addressLine1")} /></label>
            <label className="form-grid__wide">Address line 2<input value={details.addressLine2 || ""} onChange={set("addressLine2")} /></label>
            <label>City<input value={details.city || ""} onChange={set("city")} /></label>
            <label>State<input value={details.state || ""} onChange={set("state")} /></label>
            <label>Pincode<input value={details.pincode || ""} onChange={set("pincode")} />{fieldError("pincode")}</label>
          </div>

          <div className="modal__actions">
            <button type="button" onClick={continueToPayment}>Continue</button>
            <button type="button" onClick={() => { setStep(1); setInquiry(null); }}>Back</button>
          </div>
        </section>
      )}

      {step === 3 && (
        <section>
          <div className="form-grid">
            <label>
              Course *
              <select value={courseId} onChange={(e) => changeCourse(e.target.value)}>
                <option value="">Select a course...</option>
                {courses.map((c) => (
                  <option key={c.courseId} value={c.courseId}>
                    {c.name} — {money(c.fees)}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Batch *
              <select
                value={batchId}
                onChange={(e) => setBatchId(e.target.value)}
                disabled={!courseId || loadingCourse}
              >
                <option value="">
                  {loadingCourse ? "Loading batches..." : "Select a batch..."}
                </option>
                {batches.map((b) => (
                  <option key={b.batchId} value={b.batchId}>
                    {b.batchName} — {b.currentCount}/{b.capacity} filled ({b.status})
                  </option>
                ))}
              </select>
            </label>
          </div>

          {/* A switch is legitimate — people enquire about one course and sign
              up for another — but it's also an easy misclick, and it silently
              changes the fee. Saying so beats letting it pass unremarked. */}
          {courseId && inquiry.courseId && Number(courseId) !== inquiry.courseId && (
            <p className="notice">
              Enquiry #{inquiry.inquiryId} was for <strong>{inquiry.courseName}</strong>. Registering
              for <strong>{courses.find((c) => c.courseId === Number(courseId))?.name}</strong>{" "}
              instead will update the enquiry to match, and the fee below has been recalculated.
            </p>
          )}

          {courseId && !loadingCourse && batches.length === 0 && (
            <p className="error">
              No open batch exists for{" "}
              {courses.find((c) => c.courseId === Number(courseId))?.name || "this course"}. Create
              one under Batch Management, or pick a different course.
            </p>
          )}

          {fees && (
            <div className="fee-panel">
              <h4>Fee — auto-populated from {fees.courseName}</h4>
              <table>
                <tbody>
                  <tr><td>Total course fee</td><td>{money(fees.totalFees)}</td><td></td></tr>
                  <tr>
                    <td>Installment 1 of {fees.totalInstallments}</td>
                    <td>{money(fees.installment1Amount)}</td>
                    <td>due {fees.installment1DueDate}</td>
                  </tr>
                  <tr>
                    <td>Installment 2 of {fees.totalInstallments}</td>
                    <td>{money(fees.installment2Amount)}</td>
                    <td>due {fees.installment2DueDate}</td>
                  </tr>
                </tbody>
              </table>
              <p className="form-note">
                The 2-installment plan is fixed system-wide and isn't editable here.
              </p>
            </div>
          )}

          <div className="form-grid">
            <label>
              Collecting now *
              <input
                type="number"
                step="0.01"
                value={payment.amountPaid ?? ""}
                onChange={(e) => setPayment({ ...payment, amountPaid: e.target.value })}
              />
            </label>
            <label>
              Mode
              <select
                value={payment.paymentMode}
                onChange={(e) => setPayment({ ...payment, paymentMode: e.target.value })}
              >
                {PAYMENT_MODES.map((m) => <option key={m} value={m}>{m}</option>)}
              </select>
            </label>
            <label>
              Transaction ref
              <input
                value={payment.transactionId || ""}
                onChange={(e) => setPayment({ ...payment, transactionId: e.target.value })}
              />
            </label>
            <label className="form-grid__wide">
              Remarks
              <input
                value={payment.remarks || ""}
                onChange={(e) => setPayment({ ...payment, remarks: e.target.value })}
              />
            </label>
          </div>

          <div className="modal__actions">
            <button type="button" onClick={submit} disabled={submitting || !batchId}>
              {submitting ? "Registering..." : "Register & issue receipt"}
            </button>
            <button type="button" onClick={() => setStep(2)}>Back</button>
          </div>
        </section>
      )}
    </div>
  );
}
