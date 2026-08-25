/**
 * The signed-in website visitor.
 *
 * Kept out of Redux and out of authSlice deliberately. `auth` means "a staff
 * member is working in the admin panel" — it drives ProtectedRoute, the
 * admin nav and the role helpers. A visitor is a different kind of principal
 * with one privilege (submit an enquiry), and folding them into the same
 * slice would mean every `isAuthenticated` check in the admin area suddenly
 * has to ask "authenticated as what?".
 *
 * Storage keys are separate for the same reason: a staff member and a
 * visitor can be signed in in the same browser without evicting each other.
 */

export const VISITOR_TOKEN_KEY = "csk_visitor_token";
export const VISITOR_PROFILE_KEY = "csk_visitor";
/** Where to return to after the round trip through Google. */
const RETURN_TO_KEY = "csk_visitor_return_to";

export function getVisitorToken() {
  return localStorage.getItem(VISITOR_TOKEN_KEY);
}

export function getVisitor() {
  try {
    const raw = localStorage.getItem(VISITOR_PROFILE_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function saveVisitorSession({ token, email, name, expiresInMs }) {
  localStorage.setItem(VISITOR_TOKEN_KEY, token);
  localStorage.setItem(
    VISITOR_PROFILE_KEY,
    // expiresAt is stored rather than the duration: after a reload, "one
    // hour from when it was issued" is only answerable if the issue time
    // was resolved to a wall-clock instant at the time it was known.
    JSON.stringify({ email, name, expiresAt: Date.now() + Number(expiresInMs || 0) })
  );
}

export function clearVisitorSession() {
  localStorage.removeItem(VISITOR_TOKEN_KEY);
  localStorage.removeItem(VISITOR_PROFILE_KEY);
}

/**
 * True when a token exists and hasn't lapsed.
 *
 * The expiry check is a convenience so the UI can offer "sign in again"
 * before the user fills a form that's going to be rejected. The server
 * still validates the signature and expiry on every request — this is not
 * a security boundary.
 */
export function isVisitorSignedIn() {
  const token = getVisitorToken();
  if (!token) return false;

  const visitor = getVisitor();
  if (visitor?.expiresAt && Date.now() >= visitor.expiresAt) {
    clearVisitorSession();
    return false;
  }
  return true;
}

const API_BASE = import.meta.env.VITE_API_BASE_URL || "http://localhost:8080/api";

/**
 * Hands the browser to Spring's authorization endpoint.
 *
 * A full navigation, not fetch/XHR: the visitor has to actually land on
 * Google's consent screen, and the flow relies on the session cookie Spring
 * sets while it holds the PKCE verifier and `state`. An XHR would follow
 * the redirect invisibly and hand back Google's HTML.
 */
export function startGoogleSignIn(returnTo) {
  sessionStorage.setItem(RETURN_TO_KEY, returnTo || window.location.pathname);
  window.location.assign(`${API_BASE.replace(/\/$/, "")}/oauth2/authorization/google`);
}

export function consumeReturnTo(fallback = "/") {
  const target = sessionStorage.getItem(RETURN_TO_KEY);
  sessionStorage.removeItem(RETURN_TO_KEY);
  // Only same-site paths are honoured. Redirecting to an arbitrary stored
  // value is how an open redirect gets built by accident.
  return target && target.startsWith("/") && !target.startsWith("//") ? target : fallback;
}
