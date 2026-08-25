import { useEffect } from "react";
import { useParams, Link } from "react-router-dom";
import useApiResource from "../../hooks/useApiResource";
import resolveMediaUrl from "../../utils/mediaUrl";

export default function RecruiterDetail() {
  const { id } = useParams();
  const recruiterApi = useApiResource("/recruiters");
  // baseEndpoint here is the collection root; fetchById appends "/{id}",
  // giving the correct GET /placement-records/by-recruiter/{id} call.
  // The backend returns a List for this "by-id" style path, so we read
  // it back from `.item` (the hook doesn't care whether the payload
  // shape is a single object or an array — it just stores what came back).
  const placementsApi = useApiResource("/placement-records/by-recruiter");

  useEffect(() => {
    recruiterApi.fetchById(id);
    placementsApi.fetchById(id);
  }, [id]); // eslint-disable-line react-hooks/exhaustive-deps

  const recruiter = recruiterApi.item;
  const placements = placementsApi.item || [];

  return (
    <div className="recruiter-detail-page">
      <Link to="/placement/recruiters">&larr; Back to Our Recruiters</Link>
      {recruiter && (
        <div className="recruiter-detail__header">
          {recruiter.logoUrl && <img src={resolveMediaUrl(recruiter.logoUrl)} alt={recruiter.companyName} />}
          <h1>{recruiter.companyName}</h1>
        </div>
      )}

      <h2>Students Placed at {recruiter?.companyName || "this company"}</h2>
      {placementsApi.loading && <p>Loading...</p>}
      <div className="placement-grid">
        {placements.map((p) => (
          <div key={p.placementId} className="placement-card">
            {p.studentPhotoUrl && <img src={resolveMediaUrl(p.studentPhotoUrl)} alt={p.studentName} />}
            <strong>{p.studentName}</strong>
            <span>{p.position}</span>
          </div>
        ))}
        {!placementsApi.loading && placements.length === 0 && <p>No placements recorded yet.</p>}
      </div>
    </div>
  );
}
