import { useEffect } from "react";
import useApiResource from "../../hooks/useApiResource";

// BRD: "crawling text area in which various announcements will be
// displayed... Only valid items will be displayed." The validity
// filtering (active + within start/end date window) happens entirely on
// the backend (/announcements/valid) — this component just renders
// whatever comes back.
export default function Ticker() {
  const { data: announcements, loading, fetchAll } = useApiResource("/announcements/valid");

  useEffect(() => {
    fetchAll();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  if (loading || announcements.length === 0) return null;

  return (
    <div className="ticker">
      <div className="ticker__track">
        {announcements.map((a) => (
          <span key={a.announcementId} className="ticker__item">{a.content}</span>
        ))}
      </div>
    </div>
  );
}
