import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useSelector } from "react-redux";

/**
 * Gates admin routes. Two levels, deliberately:
 *
 *   <ProtectedRoute />                          -> any logged-in staff member
 *   <ProtectedRoute allowedRoles={[...]} />     -> only those roles
 *
 * The second form is what keeps Table Maintenance to Admin/Manager while
 * leaving the CRM screens open to Counselors, mirroring the server-side
 * rules in SecurityConfig. This is a UX guard, not the security boundary —
 * the API enforces the same split independently, so a hand-typed URL gets
 * a 403 from the backend regardless of what the frontend renders.
 */
export default function ProtectedRoute({ allowedRoles }) {
  const { isAuthenticated, staff } = useSelector((state) => state.auth);
  const location = useLocation();

  if (!isAuthenticated) {
    // Remember where they were headed so login can send them back.
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  if (allowedRoles && !allowedRoles.includes(staff?.role)) {
    return (
      <div className="admin-denied">
        <h2>Not available for your role</h2>
        <p>
          You're signed in as <strong>{staff?.name}</strong> ({staff?.role || "unknown role"}), which
          doesn't have access to this screen.
        </p>
      </div>
    );
  }

  return <Outlet />;
}
