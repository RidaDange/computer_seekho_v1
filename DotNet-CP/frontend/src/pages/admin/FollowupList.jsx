import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useSelector } from "react-redux";
import axiosClient from "../../api/axiosClient";
import useApiResource from "../../hooks/useApiResource";
import { MASTER_DATA_ROLES } from "../../store/slices/authSlice";

/**
 * The admin panel's landing page, per the BRD.
 *
 * Two lists, because a follow-up has two distinct states and conflating
 * them hides work:
 *
 *   Due now       — dated today or earlier. The actual call list.
 *   Scheduled ahead — booked for a future date. Not urgent, but a brand-new
 *                     enquiry lands here (its first follow-up is +3 days),
 *                     so without it a counselor would add an enquiry and
 *                     see it nowhere at all.
 *
 * Both lists can be logged from. A counselor who has the prospect on the
 * phone today shouldn't be blocked from recording it just because the
 * system had pencilled the call in for Thursday.
 */
export default function FollowupList() {
  const staff = useSelector((state) => state.auth.staff);
  // Counselors work their own pipeline; Admin/Manager oversee everyone's,
  // so their default is the whole institute. Either can switch.
  const isOverseer = MASTER_DATA_ROLES.includes(staff?.role);
  const [mineOnly, setMineOnly] = useState(!isOverseer);

  const dueApi = useApiResource("/followups/due");
  const upcomingApi = useApiResource("/followups/upcoming");

  const [activeRow, setActiveRow] = useState(null); // followup being logged
  const [outcome, setOutcome] = useState("Done");
  const [notes, setNotes] = useState("");
  const [nextDate, setNextDate] = useState("");
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState(null);
  const [confirmation, setConfirmation] = useState(null);
  const [showUpcoming, setShowUpcoming] = useState(true);

  const refresh = () => {
    dueApi.fetchAll({ mine: mineOnly });
    upcomingApi.fetchAll({ mine: mineOnly });
  };

  useEffect(() => {
    refresh();
  }, [mineOnly]); // eslint-disable-line react-hooks/exhaustive-deps

  const openLogForm = (row) => {
    setConfirmation(null);
    setActiveRow(row);
    setOutcome("Done");
    setNotes("");
    setNextDate("");
    setFormError(null);
  };

  const submitLog = async () => {
    setSaving(true);
    setFormError(null);
    try {
      await axiosClient.put(`/followups/${activeRow.followupId}/log`, {
        status: outcome,
        notes: notes.trim() || null,
        nextFollowup: nextDate || null,
      });

      // Say explicitly what happened. A logged follow-up always leaves the
      // due list, and a next one dated in the future reappears under
      // "Scheduled ahead" rather than here — that reads as a failed save
      // unless we spell it out.
      setConfirmation(
        nextDate
          ? `Logged. Next follow-up for ${activeRow.enquirerName} is booked for ${nextDate} — see "Scheduled ahead" below.`
          : `Logged. No further follow-up scheduled for ${activeRow.enquirerName}; close the enquiry from All Enquiries when you're ready.`
      );

      setActiveRow(null);
      refresh();
    } catch (err) {
      setFormError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const scopeToggle = (
    <label className="inline-check">
      <input
        type="checkbox"
        checked={mineOnly}
        onChange={(e) => setMineOnly(e.target.checked)}
      />
      Only mine
    </label>
  );

  return (
    <div className="followup-page">
      <div className="page-header">
        <h2>Follow-ups</h2>
        <div className="page-header__controls">
          {scopeToggle}
          <Link to="/admin/inquiries/new" className="button">+ Add Enquiry</Link>
        </div>
      </div>

      {confirmation && (
        <p className="notice">
          {confirmation}
          <button type="button" className="notice__dismiss" onClick={() => setConfirmation(null)}>
            Dismiss
          </button>
        </p>
      )}

      <h3 className="section-heading">Due now ({dueApi.data.length})</h3>
      {dueApi.loading && <p>Loading follow-ups...</p>}
      {dueApi.error && <p className="error">{dueApi.error}</p>}

      <table>
        <thead>
          <tr>
            <th>Due</th>
            <th>Enquirer</th>
            <th>Phone</th>
            <th>Course</th>
            <th>Counselor</th>
            <th>Notes</th>
            <th>Action</th>
          </tr>
        </thead>
        <tbody>
          {dueApi.data.map((f) => (
            <tr key={f.followupId} className={f.daysOverdue > 0 ? "row--overdue" : ""}>
              <td>
                {f.followupDate}
                {f.daysOverdue > 0 && (
                  <span className="badge badge--overdue">{f.daysOverdue}d overdue</span>
                )}
              </td>
              <td>
                <Link to={`/admin/inquiries?highlight=${f.inquiryId}`}>{f.enquirerName}</Link>
              </td>
              <td>{f.phone}</td>
              <td>{f.courseName}</td>
              <td>{f.staffName || "—"}</td>
              <td className="cell--notes">{f.notes}</td>
              <td>
                <button type="button" onClick={() => openLogForm(f)}>Log call</button>
              </td>
            </tr>
          ))}
          {!dueApi.loading && dueApi.data.length === 0 && (
            <tr>
              <td colSpan={7}>
                Nothing due today.
                {upcomingApi.data.length > 0 && " Upcoming calls are listed below — you can log one early."}
              </td>
            </tr>
          )}
        </tbody>
      </table>

      <section className="upcoming">
        <button
          type="button"
          className="upcoming__toggle"
          onClick={() => setShowUpcoming((v) => !v)}
        >
          {showUpcoming ? "▾" : "▸"} Scheduled ahead ({upcomingApi.data.length})
        </button>

        {showUpcoming && (
          <table>
            <thead>
              <tr>
                <th>Due</th>
                <th>Enquirer</th>
                <th>Phone</th>
                <th>Course</th>
                <th>Counselor</th>
                <th>Notes</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {upcomingApi.data.map((f) => (
                <tr key={f.followupId}>
                  <td>{f.followupDate}</td>
                  <td>
                    <Link to={`/admin/inquiries?highlight=${f.inquiryId}`}>{f.enquirerName}</Link>
                  </td>
                  <td>{f.phone}</td>
                  <td>{f.courseName}</td>
                  <td>{f.staffName || "—"}</td>
                  <td className="cell--notes">{f.notes}</td>
                  <td>
                    <button type="button" onClick={() => openLogForm(f)}>Log call</button>
                  </td>
                </tr>
              ))}
              {upcomingApi.data.length === 0 && (
                <tr>
                  <td colSpan={7}>Nothing booked for a future date.</td>
                </tr>
              )}
            </tbody>
          </table>
        )}
      </section>

      {activeRow && (
        <div className="modal">
          <h3>Log follow-up — {activeRow.enquirerName}</h3>
          <p className="modal__meta">
            {activeRow.courseName} · {activeRow.phone} · {activeRow.email}
          </p>

          {formError && <p className="error">{formError}</p>}

          <label>
            Outcome
            <select value={outcome} onChange={(e) => setOutcome(e.target.value)}>
              <option value="Done">Spoke to them (Done)</option>
              <option value="No Response">No response</option>
            </select>
          </label>

          <label>
            Notes
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="What was discussed?"
            />
          </label>

          <label>
            Next follow-up
            <input type="date" value={nextDate} onChange={(e) => setNextDate(e.target.value)} />
          </label>
          <p className="form-note">
            Leave the date blank to end the follow-up trail — do that when you're about to close
            the enquiry instead.
          </p>

          <div className="modal__actions">
            <button type="button" onClick={submitLog} disabled={saving}>
              {saving ? "Saving..." : "Save"}
            </button>
            <button type="button" onClick={() => setActiveRow(null)}>Cancel</button>
          </div>
        </div>
      )}
    </div>
  );
}
