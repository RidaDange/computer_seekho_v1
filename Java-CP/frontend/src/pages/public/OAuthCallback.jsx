import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { consumeReturnTo, saveVisitorSession } from "../../utils/visitorSession";

const MESSAGES = {
  no_email: "Google didn't share an email address, so we can't file an enquiry against it.",
  email_unverified: "That Google account's email address isn't verified.",
  signin_failed: "Sign-in didn't complete.",
};

/**
 * Where Google sends the visitor back to, by way of our own success handler.
 *
 * The token arrives as a query parameter, which means it is briefly in the
 * address bar and would otherwise stay in browser history. It's read once
 * and the URL is rewritten immediately with replaceState — same page, no
 * navigation, no history entry holding a credential.
 */
export default function OAuthCallback() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const [error, setError] = useState(null);

  useEffect(() => {
    const token = params.get("token");
    const failure = params.get("error");

    // Scrub before anything else. If a later step throws, the credential is
    // already out of the URL rather than left there by the unwind.
    window.history.replaceState({}, "", window.location.pathname);

    if (failure || !token) {
      setError(MESSAGES[failure] || MESSAGES.signin_failed);
      return;
    }

    saveVisitorSession({
      token,
      email: params.get("email") || "",
      name: params.get("name") || "",
      expiresInMs: params.get("expiresInMs"),
    });

    // replace, not push: the back button should take the visitor to the page
    // they started from, not back through this callback and round the loop
    // again.
    navigate(consumeReturnTo("/enquiry"), { replace: true });
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  if (error) {
    return (
      <div className="page-narrow">
        <h2>Sign-in didn't work</h2>
        <p className="error">{error}</p>
        <p>
          <button type="button" className="button" onClick={() => navigate("/enquiry")}>
            Try again
          </button>
        </p>
      </div>
    );
  }

  return (
    <div className="page-narrow">
      <p>Signing you in...</p>
    </div>
  );
}
