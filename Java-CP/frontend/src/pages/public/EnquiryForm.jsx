import { useEffect, useState } from "react";
import { useSearchParams, Link } from "react-router-dom";
import axiosClient, { TOKEN_KEY } from "../../api/axiosClient";
import useApiResource from "../../hooks/useApiResource";
import EnquiryFields from "../../components/common/EnquiryFields";
import { validateEnquiry } from "../../utils/enquiryValidation";
import {
  clearVisitorSession,
  getVisitor,
  isVisitorSignedIn,
  startGoogleSignIn,
} from "../../utils/visitorSession";

/**
 * The visitor-facing enquiry form.
 *
 * Browsing the site needs no account, but submitting an enquiry does: the
 * visitor signs in with Google first, and the enquiry is filed against the
 * address Google verified. Without that, anyone could file enquiries under
 * anyone's address, and the confirmation email would land on a stranger.
 *
 * The gate is only ever a courtesy — POST /inquiries/public requires
 * authentication server-side, so hiding the form is about not letting
 * someone fill in sixteen fields before being told to log in.
 */
export default function EnquiryForm() {
  const [params] = useSearchParams();
  const { data: courses, fetchAll } = useApiResource("/courses/active");

  // A staff member browsing the public site is already identified, so they
  // aren't sent round to Google to prove it a second time. The server takes
  // the same view.
  const isStaff = Boolean(localStorage.getItem(TOKEN_KEY));
  const [signedIn, setSignedIn] = useState(() => isVisitorSignedIn() || isStaff);
  const visitor = getVisitor();

  const [form, setForm] = useState({
    courseId: params.get("course") || "",
    // Prefilled from the Google profile. The name stays editable — the
    // account holder isn't always the prospective student, and Google's
    // display name is often not what someone would write on a form.
    enquirerName: visitor?.name || "",
    email: visitor?.email || "",
  });
  const [fieldErrors, setFieldErrors] = useState({});
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);
  const [reference, setReference] = useState(null);

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
      const { data } = await axiosClient.post("/inquiries/public", {
        courseId: Number(form.courseId),
        enquirerName: form.enquirerName.trim(),
        email: form.email.trim(),
        phone: form.phone.trim(),
        message: form.message?.trim() || null,
        source: "Website",
      });
      setReference(data.inquiryId);
    } catch (err) {
      // A 401 here means the hour-long visitor token lapsed while the form
      // was open. The interceptor has already cleared it; re-gating the page
      // turns a dead-end error into the sign-in button they need.
      if (err.status === 401) {
        setSignedIn(false);
        setError("Your sign-in expired. Please sign in again — your answers are still here.");
      } else {
        setError(err.message);
      }
    } finally {
      setSubmitting(false);
    }
  };

  if (reference) {
    return (
      <div className="enquiry-page">
        <h1>Thank you</h1>
        <p>
          Your enquiry has been received — reference <strong>#{reference}</strong>. A confirmation
          email is on its way to <strong>{form.email}</strong>, and one of our counsellors will be
          in touch shortly.
        </p>
        <Link to="/" className="button">Back to home</Link>
      </div>
    );
  }

  if (!signedIn) {
    return (
      <div className="enquiry-page">
        <h1>Enquire about a course</h1>
        <p>
          Sign in with Google to continue. We use it only to confirm your email address, so we can
          send your enquiry reference and reply to the right person.
        </p>

        {error && <p className="error">{error}</p>}

        <button
          type="button"
          className="button button--google"
          onClick={() => startGoogleSignIn("/enquiry")}
        >
          Sign in with Google
        </button>

        <p className="form-note">
          You don't need an account to browse courses, faculty or placements — only to send an
          enquiry.
        </p>
      </div>
    );
  }

  return (
    <div className="enquiry-page">
      <h1>Enquire about a course</h1>
      <p>Tell us what you're interested in and we'll get back to you.</p>

      {visitor?.email && (
        <p className="notice">
          Signed in as <strong>{visitor.email}</strong>.{" "}
          <button
            type="button"
            className="link-button"
            onClick={() => {
              clearVisitorSession();
              setSignedIn(false);
            }}
          >
            Use a different account
          </button>
        </p>
      )}

      {error && <p className="error">{error}</p>}

      <form onSubmit={handleSubmit}>
        <EnquiryFields
          form={form}
          setForm={setForm}
          courses={courses}
          fieldErrors={fieldErrors}
          readOnlyFields={visitor?.email ? ["email"] : []}
        />
        <button type="submit" disabled={submitting}>
          {submitting ? "Sending..." : "Submit enquiry"}
        </button>
      </form>
    </div>
  );
}
