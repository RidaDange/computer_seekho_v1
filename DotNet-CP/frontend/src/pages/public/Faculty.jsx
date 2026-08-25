import { useEffect } from "react";
import useApiResource from "../../hooks/useApiResource";
import resolveMediaUrl from "../../utils/mediaUrl";

export default function Faculty() {
  const { data: faculty, loading, error, fetchAll } = useApiResource("/staff/by-role/Faculty");

  useEffect(() => {
    fetchAll();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <div className="faculty-page">
      <h1>Our Faculty</h1>
      {loading && <p>Loading faculty...</p>}
      {error && <p className="error">{error}</p>}
      <div className="faculty-grid">
        {faculty.map((f) => (
          <div key={f.staffId} className="faculty-card">
            {f.photoUrl && <img src={resolveMediaUrl(f.photoUrl)} alt={f.name} />}
            <h3>{f.name}</h3>
            {f.qualification && <p>{f.qualification}</p>}
            {f.experience != null && <p>{f.experience} years experience</p>}
          </div>
        ))}
        {!loading && faculty.length === 0 && <p>No faculty listed yet.</p>}
      </div>
    </div>
  );
}
