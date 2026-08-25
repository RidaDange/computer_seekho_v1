import { useState } from "react";
import { useDispatch } from "react-redux";
import { useLocation, useNavigate, Link } from "react-router-dom";
import axiosClient from "../../api/axiosClient";
import { loginSuccess } from "../../store/slices/authSlice";

export default function Login() {
  const [form, setForm] = useState({ username: "", password: "" });
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  const dispatch = useDispatch();
  const navigate = useNavigate();
  const location = useLocation();

  // Set by axiosClient when it bounces an expired token back here.
  const sessionExpired = new URLSearchParams(location.search).get("expired") === "1";
  const redirectTo = location.state?.from || "/admin";

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);

    if (!form.username.trim() || !form.password) {
      setError("Enter both your username and password.");
      return;
    }

    setSubmitting(true);
    try {
      const { data } = await axiosClient.post("/auth/login", {
        username: form.username.trim(),
        password: form.password,
      });
      dispatch(loginSuccess({ staff: data.staff, token: data.token }));
      navigate(redirectTo, { replace: true });
    } catch (err) {
      setError(err.message || "Login failed. Please try again.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="login-page">
      <form className="login-card" onSubmit={handleSubmit}>
        <h1>Staff Login</h1>
        <p className="login-card__subtitle">SMVITA — ComputerSeekho admin panel</p>

        {sessionExpired && !error && (
          <p className="notice">Your session expired. Please sign in again.</p>
        )}
        {error && <p className="error">{error}</p>}

        <label>
          Username
          <input
            autoFocus
            autoComplete="username"
            value={form.username}
            onChange={(e) => setForm({ ...form, username: e.target.value })}
          />
        </label>

        <label>
          Password
          <input
            type="password"
            autoComplete="current-password"
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
          />
        </label>

        <button type="submit" disabled={submitting}>
          {submitting ? "Signing in..." : "Sign in"}
        </button>

        <Link to="/" className="login-card__back">&larr; Back to the website</Link>
      </form>
    </div>
  );
}
