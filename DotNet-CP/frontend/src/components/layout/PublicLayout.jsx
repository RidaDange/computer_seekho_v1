import { useEffect, useState } from "react";
import { Outlet, Link, useLocation } from "react-router-dom";

/**
 * Chrome for the public website: sticky header, content, footer.
 *
 * The header carries a Staff Login entry alongside Enquire Now. Staff reach
 * the admin panel from the same site visitors use — there's no separate
 * address to remember — but it's styled as a quiet outline next to the red
 * call to action, because a prospective student is who the page is for and
 * "Enquire Now" has to stay the obvious thing to click.
 */
export default function PublicLayout() {
  const [placementMenuOpen, setPlacementMenuOpen] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const location = useLocation();

  // On a phone the nav is an off-canvas panel, and tapping a link inside it
  // changes route without unmounting this component — so without this the
  // panel stays open on top of the page you just navigated to.
  useEffect(() => {
    setMenuOpen(false);
    setPlacementMenuOpen(false);
  }, [location.pathname]);

  return (
    <div className="public-layout">
      <header className="public-nav">
        <Link to="/" className="public-nav__brand">
          Computer<span>Seekho</span>
        </Link>

        <button
          type="button"
          className="public-nav__toggle"
          aria-expanded={menuOpen}
          aria-label="Toggle navigation"
          onClick={() => setMenuOpen((open) => !open)}
        >
          ☰
        </button>

        <nav className={menuOpen ? "is-open" : ""}>
          <Link to="/">Home</Link>

          <div
            className="public-nav__dropdown"
            onMouseEnter={() => setPlacementMenuOpen(true)}
            onMouseLeave={() => setPlacementMenuOpen(false)}
          >
            {/* Click as well as hover: hover doesn't exist on a touch
                screen, so the submenu would be unreachable there. */}
            <span onClick={() => setPlacementMenuOpen((open) => !open)}>Placement ▾</span>
            {placementMenuOpen && (
              <div className="public-nav__dropdown-menu">
                <Link to="/placement/batchwise">Batchwise Placement</Link>
                <Link to="/placement/recruiters">Our Recruiters</Link>
              </div>
            )}
          </div>

          <Link to="/campus-life">Campus Life</Link>
          <Link to="/faculty">Faculty</Link>
          <Link to="/get-in-touch">Get in Touch</Link>
          <Link to="/enquiry" className="public-nav__cta">Enquire Now</Link>
          <Link to="/login" className="public-nav__staff">Staff Login</Link>
        </nav>
      </header>

      <main className="public-content">
        <Outlet />
      </main>

      <footer className="public-footer">
        <div className="public-footer__inner">
          <div>
            <p className="public-footer__brand">ComputerSeekho</p>
            {/* Taken from the letterhead on the BRD's Annexure 1 enrolment
                form, so the site and the printed paperwork agree. */}
            <p>
              USM&apos;s Shriram Mantri Vidyanidhi Info Tech Academy<br />
              Authorised Training Centre of C-DAC ACTS
            </p>
            <p>
              5th Floor, Vidyanidhi Education Complex,<br />
              Vidyanidhi Marg, JVPD Scheme, Juhu,<br />
              Mumbai — 400 049
            </p>
          </div>

          <div>
            <h4>Reach us at</h4>
            <p>Tel: 022-26255629 / 2670 5498</p>
            <p><a href="mailto:training@vita.com">training@vita.com</a></p>
          </div>

          <div>
            <h4>Quick links</h4>
            <Link to="/campus-life">Campus Life</Link>
            <Link to="/faculty">Faculty</Link>
            <Link to="/placement/batchwise">Placements</Link>
            <Link to="/get-in-touch">Get in Touch</Link>
            <Link to="/enquiry">Enquire Now</Link>
          </div>
        </div>

        <div className="public-footer__bottom">
          <span>&copy; {new Date().getFullYear()} SMVITA — ComputerSeekho</span>
          <Link to="/login">Staff Login</Link>
        </div>
      </footer>
    </div>
  );
}
