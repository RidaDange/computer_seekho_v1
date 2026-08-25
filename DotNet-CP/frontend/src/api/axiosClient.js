import axios from "axios";

// Single Axios instance for the whole app — every request/response
// concern (base URL, auth header, error normalization) lives here once,
// instead of being repeated at each call site.
const axiosClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "http://localhost:8080/api",
  headers: {
    "Content-Type": "application/json",
  },
});

export const TOKEN_KEY = "csk_token";
export const STAFF_KEY = "csk_staff";

// Duplicated from utils/visitorSession rather than imported, to keep this
// module free of app-level imports — visitorSession imports the API base URL
// from here, and a cycle between them would be resolved at module-load time
// with one of the two still undefined.
const VISITOR_TOKEN_KEY = "csk_visitor_token";
const VISITOR_PROFILE_KEY = "csk_visitor";

// Attach the JWT to every request.
//
// Two kinds of principal can be signed in at once — a staff member in the
// admin panel and a visitor on the public site — so the staff token wins
// when both are present. Staff already satisfy every rule a visitor does,
// and preferring the visitor token would silently downgrade an admin's
// privileges on any shared endpoint.
axiosClient.interceptors.request.use((config) => {
  const token =
    localStorage.getItem(TOKEN_KEY) || localStorage.getItem(VISITOR_TOKEN_KEY);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Normalize error shape so every screen can rely on `err.message`
// without re-parsing Axios's response structure each time.
axiosClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error.response?.status;

    // An expired or revoked token: clear it and bounce to the login screen
    // once, centrally. Doing this per-screen would mean every admin page
    // needing its own copy of the same check.
    //
    // The login call itself is exempt — a wrong password there is a normal
    // form error the Login page renders inline, not a session expiry.
    const isLoginAttempt = error.config?.url?.includes("/auth/login");
    if (status === 401 && !isLoginAttempt) {
      // Which session lapsed decides what happens next. A staff member gets
      // bounced to the admin login; a visitor on the public site must not,
      // because the staff login page is not somewhere they can act — the
      // enquiry form simply re-offers the Google button once its token is
      // gone.
      if (localStorage.getItem(TOKEN_KEY)) {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(STAFF_KEY);
        if (!window.location.pathname.startsWith("/login")) {
          window.location.assign("/login?expired=1");
        }
      } else {
        localStorage.removeItem(VISITOR_TOKEN_KEY);
        localStorage.removeItem(VISITOR_PROFILE_KEY);
      }
    }

    const body = error.response?.data;

    // Bean Validation failures come back as { message: "Validation failed",
    // fieldErrors: ["phone: Enter a valid...", ...] }. Reading only `message`
    // discards the half that says what to fix, so the two are joined here —
    // once, centrally, rather than every screen remembering to check.
    const fieldErrors = Array.isArray(body?.fieldErrors) ? body.fieldErrors : null;

    const message =
      [body?.message, fieldErrors?.join(" · ")].filter(Boolean).join(" — ") ||
      body?.error ||
      (status === 403 ? "Your role doesn't have access to this action." : null) ||
      error.message ||
      "Something went wrong. Please try again.";

    return Promise.reject({ ...error, message, status, fieldErrors });
  }
);

export default axiosClient;
