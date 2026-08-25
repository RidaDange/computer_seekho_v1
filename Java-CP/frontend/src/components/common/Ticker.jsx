import { useEffect } from "react"; //when this component opens, announcements load automatically
import useApiResource from "../../hooks/useApiResource"; //custom hook

// BRD: "crawling text area in which various announcements will be
// displayed... Only valid items will be displayed." The validity
// filtering (active + within start/end date window) happens entirely on
// the backend (/announcements/valid) — this component just renders
// whatever comes back.
export default function Ticker() {
  //creating component
  const {
    data: announcements, //data contains announcement list
    loading, //returns boolean value
    fetchAll, //this calls the API, without this nothing comes from backend
  } = useApiResource("/announcements/valid");

  useEffect(() => {
    fetchAll();
  }, []); //[] this is bcz api should be called only once when the component mounts

  if (loading || announcements.length === 0) return null;
  //=== strict equality op, checks whether both values & data type are exactly same.

  return (
    <div className="ticker">
      <div className="ticker__track">
        {announcements.map((a) => (
          <span key={a.announcementId} className="ticker__item">
            {a.content}
          </span>
        ))}
      </div>
    </div>
  );
}
