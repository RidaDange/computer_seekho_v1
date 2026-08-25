import { useEffect } from "react";
import { Link } from "react-router-dom";
import useApiResource from "../../hooks/useApiResource";

export default function BatchwisePlacement() {
  const { data: batches, loading, error, fetchAll } = useApiResource("/batches/completed-for-placement");

  useEffect(() => {
    fetchAll();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Group batches by category (e.g. "PG-DAC Placement", "PG-DBDA Placement")
  // to match the BRD's Batchwise Placement page layout.
  const grouped = batches.reduce((acc, batch) => {
    const key = batch.categoryName || "Other";
    (acc[key] = acc[key] || []).push(batch);
    return acc;
  }, {});

  return (
    <div className="batchwise-placement-page">
      <h1>Batchwise Placement</h1>
      {loading && <p>Loading...</p>}
      {error && <p className="error">{error}</p>}

      {Object.entries(grouped).map(([category, categoryBatches]) => (
        <section key={category}>
          <h2>{category} Placement</h2>
          <div className="batch-grid">
            {categoryBatches.map((b) => {
              const pct = b.capacity ? Math.round((b.placedCount / b.capacity) * 100) : 0;
              return (
                <Link key={b.batchId} to={`/placement/batchwise/${b.batchId}`} className="batch-card">
                  <strong>{b.batchName}</strong>
                  <span>{pct}% Placement</span>
                  <span className="batch-card__count">{b.placedCount}/{b.capacity} placed</span>
                </Link>
              );
            })}
          </div>
        </section>
      ))}

      {!loading && batches.length === 0 && <p>No completed batches with placement data yet.</p>}
    </div>
  );
}
