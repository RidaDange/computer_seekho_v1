import { useEffect } from "react";
import { useParams, Link } from "react-router-dom";
import useApiResource from "../../hooks/useApiResource";
import resolveMediaUrl from "../../utils/mediaUrl";

export default function PlacementDetail() {
  const { batchId } = useParams();
  const batchApi = useApiResource("/batches");
  const placementsApi = useApiResource("/placement-records/by-batch");

  useEffect(() => {
    batchApi.fetchById(batchId);
    placementsApi.fetchById(batchId);
  }, [batchId]); // eslint-disable-line react-hooks/exhaustive-deps

  const batch = batchApi.item;
  const placements = placementsApi.item || [];

  return (
    <div className="placement-detail-page">
      <Link to="/placement/batchwise">&larr; Back to Batchwise Placement</Link>
      <h1>{batch?.batchName || "Batch"}</h1>

      {placementsApi.loading && <p>Loading...</p>}
      <div className="placement-grid">
        {placements.map((p) => (
          <div key={p.placementId} className="placement-card">
            {p.studentPhotoUrl && <img src={resolveMediaUrl(p.studentPhotoUrl)} alt={p.studentName} />}
            <strong>{p.studentName}</strong>
            <span>{p.recruiterCompanyName}</span>
          </div>
        ))}
        {!placementsApi.loading && placements.length === 0 && <p>No placements recorded for this batch yet.</p>}
      </div>
    </div>
  );
}
