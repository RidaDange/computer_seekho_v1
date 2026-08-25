import { useEffect } from "react";
import { Outlet, Link, NavLink, useNavigate, useLocation } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import { logout, MASTER_DATA_ROLES, CRM_ROLES, hasRole } from "../../store/slices/authSlice";
import useApiResource from "../../hooks/useApiResource";

// Nav is filtered by role rather than showing links that 403 on click.
// The same split is enforced server-side in SecurityConfig — this is just
// the polite version of it.
const MASTER_TABLES = [
  ["course-categories", "Course Categories"],
  ["courses", "Courses"],
  ["staff", "Staff"],
  ["recruiters", "Recruiters"],
  ["announcements", "Announcements"],
  ["closure-reasons", "Closure Reasons"],
  ["banners", "Banners"],
  ["testimonials", "Testimonials"],
  ["news-events", "News & Events"],
  ["gallery-images", "Gallery Images"],
];

export default function AdminLayout() {
  const staff = useSelector((state) => state.auth.staff);
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const location = useLocation();

  const role = staff?.role;
  const canUseCrm = hasRole(role, CRM_ROLES);
  const canEditMasterData = hasRole(role, MASTER_DATA_ROLES);

  // Live count of calls waiting, so the sidebar reflects actual work rather
  // than being a static list of links. Scoped the same way the Follow-up
  // page scopes by default: a counselor's own queue, an overseer's whole
  // institute. Re-fetched on navigation so logging a call updates it.
  const dueApi = useApiResource("/followups/due");
  const dueCount = dueApi.data.length;

  useEffect(() => {
    if (canUseCrm) {
      dueApi.fetchAll({ mine: !canEditMasterData });
    }
  }, [location.pathname, canUseCrm, canEditMasterData]); // eslint-disable-line react-hooks/exhaustive-deps

  const handleLogout = () => {
    dispatch(logout());
    navigate("/login", { replace: true });
  };

  return (
    <div className="admin-layout">
      <aside className="admin-layout__sidebar">
        {/* The brand sits in the sidebar rather than the topbar so the navy
            column reads as one block from the top of the page, and the
            topbar is left for who you are and what you can do about it. */}
        <Link to="/" className="admin-layout__brand">
          Computer<span>Seekho</span>
        </Link>

        {canUseCrm && (
          <nav>
            <h4>Enquiries</h4>
            <NavLink to="/admin/followups" end>
              Follow-ups
              {dueCount > 0 && <span className="nav-badge">{dueCount}</span>}
            </NavLink>
            <NavLink to="/admin/inquiries" end>All Enquiries</NavLink>
            <NavLink to="/admin/inquiries/new">Add Enquiry</NavLink>
          </nav>
        )}

        {canUseCrm && (
          <nav>
            <h4>Students</h4>
            <NavLink to="/admin/registration">Register Student</NavLink>
            <NavLink to="/admin/students" end>Students &amp; Fees</NavLink>
          </nav>
        )}

        {canEditMasterData && (
          <nav>
            <h4>Operations</h4>
            <NavLink to="/admin/batches">Batch Management</NavLink>
            <NavLink to="/admin/placements">Placements</NavLink>
          </nav>
        )}

        {canEditMasterData && (
          <nav>
            <h4>Table Maintenance</h4>
            {MASTER_TABLES.map(([slug, label]) => (
              <NavLink key={slug} to={`/admin/table-maintenance/${slug}`}>{label}</NavLink>
            ))}
          </nav>
        )}

        {!canUseCrm && !canEditMasterData && (
          <p className="admin-layout__note">
            Your role has read-only access. Ask an administrator if you need more.
          </p>
        )}
      </aside>

      <div className="admin-layout__main">
        <header className="admin-layout__topbar">
          <div className="admin-layout__user">
            <Link to="/">View public site ↗</Link>
            <span>
              <strong>{staff?.name}</strong> · {role}
            </span>
            <button type="button" onClick={handleLogout}>Log out</button>
          </div>
        </header>

        <main className="admin-layout__content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
