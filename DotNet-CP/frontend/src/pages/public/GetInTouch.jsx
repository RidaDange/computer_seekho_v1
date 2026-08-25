import { useState } from "react";
import useApiResource from "../../hooks/useApiResource";

const MESSAGE_LIMIT = 500;

/**
 * The campus, for the map embed.
 *
 * This uses the plain maps.google.com/?output=embed form rather than the
 * official Maps Embed API, and that is a deliberate choice: the Embed API
 * requires a billable key, and a key placed in a React app ships to every
 * visitor's browser where anyone can read it and spend against it. A map of
 * a fixed public address is not worth that exposure.
 *
 * The address is written out here rather than fetched from the backend
 * because it is one fixed campus — the same string already appears in the
 * <address> block below. Putting it behind an API endpoint would add a
 * request, a controller and a table row to serve a constant.
 */
const CAMPUS_ADDRESS =
  "Vidyanidhi Education Complex, Vidyanidhi Road, Juhu Scheme, Andheri West, Mumbai 400058";

const MAP_EMBED_URL =
  `https://maps.google.com/maps?q=${encodeURIComponent(CAMPUS_ADDRESS)}&z=16&output=embed`;

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

          {/* The iframe is wrapped so the aspect ratio and rounded corners
              live on the wrapper. Styling an iframe directly is unreliable —
              border-radius in particular is ignored by some engines unless
              the parent clips it with overflow: hidden. */}
          <div className="map-embed">
            <iframe
              src={MAP_EMBED_URL}
              title={`Map showing ${CAMPUS_ADDRESS}`}
              // Deferred until it is near the viewport. The map is the
              // heaviest thing on this page and it sits below the address,
              // so it should not compete with the contact form for bandwidth.
              loading="lazy"
              // Google needs the referrer to serve the tiles, but sending a
              // full HTTPS referrer to an HTTP destination would leak it.
              referrerPolicy="no-referrer-when-downgrade"
              allowFullScreen
            />
          </div>

          <p className="map-embed__link">
            <a
              href={`https://maps.google.com/?q=${encodeURIComponent(CAMPUS_ADDRESS)}`}
              target="_blank"
              // noopener is the one that matters: without it the opened tab
              // gets a handle on this window via window.opener and can
              // navigate it elsewhere.
              rel="noopener noreferrer"
            >
              Open in Google Maps
            </a>
          </p>
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
