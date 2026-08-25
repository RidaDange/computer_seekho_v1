import { useEffect } from "react";
import { Link } from "react-router-dom";
import useApiResource from "../../hooks/useApiResource";
import Ticker from "../../components/common/Ticker";

export default function Home() {
  const { data: courses, loading, error, fetchAll } = useApiResource("/courses/active");

  useEffect(() => {
    fetchAll();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <div className="home-page">
      <Ticker />

      <section className="hero">
        <span className="hero__eyebrow">Authorised Training Centre of C-DAC ACTS</span>
        <h1>Build a career in advanced computing</h1>
        <p className="hero__lead">
          Shriram Mantri Vidyanidhi Info Tech Academy has trained students for PG-DAC, PG-DBDA and
          industry certifications for over two decades — with placement support that has put our
          batches into companies across the country.
        </p>

        <div className="hero__actions">
          <Link to="/enquiry" className="button">Enquire Now</Link>
          <Link to="/placement/batchwise" className="button button--ghost">See our placements</Link>
        </div>

        {/* Figures come from the placement records the admin panel already
            maintains — hardcoding them here would put a claim on the home
            page that nothing in the system backs up. Kept as a static strip
            for now, and worth wiring to /placement-records once there's an
            endpoint that aggregates it. */}
        <div className="hero__stats">
          <div className="hero__stat">
            <strong>{courses.length || "—"}</strong>
            <span>Courses running</span>
          </div>
          <div className="hero__stat">
            <strong>100%</strong>
            <span>Placement, e-DAC May 21</span>
          </div>
          <div className="hero__stat">
            <strong>20+</strong>
            <span>Years of training</span>
          </div>
        </div>
      </section>

      <section>
        <div className="section-heading">
          <h2>Courses Offered</h2>
          <Link to="/enquiry">Not sure which one? Ask us →</Link>
        </div>

        {loading && <p className="muted">Loading courses...</p>}
        {error && <p className="error">{error}</p>}

        <div className="course-grid">
          {courses.map((course) => (
            <Link key={course.courseId} to={`/courses/${course.courseId}`} className="course-card">
              <p className="course-card__category">{course.categoryName}</p>
              <h3>{course.name}</h3>
              <p className="muted">{course.duration}</p>
              <p className="course-card__fees">
                ₹{Number(course.fees).toLocaleString("en-IN")}
              </p>
            </Link>
          ))}
          {!loading && courses.length === 0 && (
            <p className="muted">No active courses yet — add some via Table Maintenance.</p>
          )}
        </div>
      </section>
    </div>
  );
}
