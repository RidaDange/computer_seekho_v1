import { useState } from "react";
import useApiResource from "../../hooks/useApiResource";

const MESSAGE_LIMIT = 500;

export default function GetInTouch() {
  const { loading, error, create } = useApiResource("/contact-messages");
  const [form, setForm] = useState({ name: "", email: "", message: "" });
  const [submitted, setSubmitted] = useState(false);
  const [validationError, setValidationError] = useState(null);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setValidationError(null);

    // Client-side validation mirrors the backend's @NotBlank/@Email/@Size
    // rules — the backend still enforces these regardless, this is just
    // for immediate feedback per the BRD's "necessary validations" note.
    if (!form.name.trim() || !form.email.trim() || !form.message.trim()) {
      setValidationError("Name, email, and message are all required.");
      return;
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) {
      setValidationError("Please enter a valid email address.");
      return;
    }
    if (form.message.length > MESSAGE_LIMIT) {
      setValidationError(`Message must be ${MESSAGE_LIMIT} characters or fewer.`);
      return;
    }

    try {
      await create(form);
      setSubmitted(true);
      setForm({ name: "", email: "", message: "" });
    } catch {
      // `error` from the hook already surfaces the backend's message below
    }
  };

  return (
    <div className="get-in-touch-page">
      <h1>Get in Touch</h1>

      <div className="get-in-touch__grid">
        <section>
          <h2>Our Origin</h2>
          <p>
            We are a part of Ujwanagar Shikshan Mandal (USM), a pioneering educational trust in the
            western suburbs of Mumbai. Commencing in 1958, USM has blossomed into 14 educational
            institutes that impart quality education from primary school to Post-Graduate courses.
          </p>

          <h2>Reach us at</h2>
          <address>
            Authorised Training Centre<br />
            5th Floor, Vidyanidhi Education Complex,<br />
            Vidyanidhi Road, Juhu Scheme Andheri (W),<br />
            Mumbai 400 058, India<br />
            Email: training@vita.com
          </address>

          {/* Real Google Maps embed goes here once an API key/embed URL is
              provided — placeholder for now so the page layout is complete. */}
          <div className="map-placeholder">Map</div>
        </section>

        <section>
          <h2>Get In Touch With Us!</h2>
          {submitted ? (
            <p className="notice notice--important">
              Thanks — your message has been sent. We'll get back to you soon.
            </p>
          ) : (
            <form onSubmit={handleSubmit} className="form-fields">
              <label>
                Name*
                <input
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                />
              </label>
              <label>
                Email*
                <input
                  type="email"
                  value={form.email}
                  onChange={(e) => setForm({ ...form, email: e.target.value })}
                />
              </label>
              <label>
                Message* ({form.message.length}/{MESSAGE_LIMIT})
                <textarea
                  maxLength={MESSAGE_LIMIT}
                  value={form.message}
                  onChange={(e) => setForm({ ...form, message: e.target.value })}
                  rows={5}
                />
              </label>
              {(validationError || error) && <p className="error">{validationError || error}</p>}
              <button type="submit" disabled={loading}>
                {loading ? "Sending..." : "Send Message"}
              </button>
            </form>
          )}
        </section>
      </div>
    </div>
  );
}
