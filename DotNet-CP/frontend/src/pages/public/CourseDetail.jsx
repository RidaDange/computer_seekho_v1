import { useEffect } from "react";
import { useParams, Link } from "react-router-dom";
import useApiResource from "../../hooks/useApiResource";

export default function CourseDetail() {
  const { id } = useParams();
  const { item: course, loading, error, fetchById } = useApiResource("/courses");

  useEffect(() => {
    fetchById(id);
  }, [id]); // eslint-disable-line react-hooks/exhaustive-deps

  if (loading) return <p>Loading course...</p>;
  if (error) return <p className="error">{error}</p>;
  if (!course) return null;

  return (
    <div className="course-detail">
      <Link to="/">&larr; Back to courses</Link>
      <h1>{course.name}</h1>
      <p className="course-detail__category">{course.categoryName}</p>

      <div className="course-detail__meta">
        <span><strong>Duration:</strong> {course.duration || "—"}</span>
        <span><strong>Level:</strong> {course.level}</span>
        <span><strong>Fees:</strong> ₹{Number(course.fees).toLocaleString("en-IN")}</span>
      </div>

      {course.primaryFacultyName && (
        <p><strong>Faculty:</strong> {course.primaryFacultyName}</p>
      )}

      {course.description && (
        <div className="course-detail__description">
          <h2>About this course</h2>
          <p>{course.description}</p>
        </div>
      )}

      {course.syllabusUrl && (
        <a href={course.syllabusUrl} target="_blank" rel="noreferrer">View Syllabus</a>
      )}

      <div className="course-detail__cta">
        {/* Pre-selects this course on the enquiry form, so the visitor
            doesn't have to find it again in the dropdown. */}
        <Link to={`/enquiry?course=${id}`} className="button">Enquire Now</Link>
      </div>
    </div>
  );
}
