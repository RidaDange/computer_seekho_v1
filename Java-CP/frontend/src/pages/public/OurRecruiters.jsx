import { useEffect } from "react";
import { Link } from "react-router-dom";
import useApiResource from "../../hooks/useApiResource";
import resolveMediaUrl from "../../utils/mediaUrl";

export default function OurRecruiters() {
  const { data: recruiters, loading, error, fetchAll } = useApiResource("/recruiters/active");

  useEffect(() => {
    fetchAll();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <div className="recruiters-page">
      <h1>Major Recruiters</h1>
      {loading && <p>Loading recruiters...</p>}
      {error && <p className="error">{error}</p>}
      <div className="recruiter-grid">
        {recruiters.map((r) => (
          <Link key={r.recruiterId} to={`/placement/recruiters/${r.recruiterId}`} className="recruiter-logo">
            {r.logoUrl ? <img src={resolveMediaUrl(r.logoUrl)} alt={r.companyName} /> : <span>{r.companyName}</span>}
          </Link>
        ))}
        {!loading && recruiters.length === 0 && <p>No recruiters listed yet.</p>}
      </div>
    </div>
  );
}
