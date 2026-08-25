import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { useSelector } from "react-redux";
import axiosClient from "../../api/axiosClient";
import ConfirmDialog from "../../components/common/ConfirmDialog";
import useApiResource from "../../hooks/useApiResource";
import { MASTER_DATA_ROLES } from "../../store/slices/authSlice";

const CLOSING_STATUSES = ["Lost", "Not Interested"];

/**
 * Enquiry list with the close-enquiry flow.
 *
 * "Open only" is the default view because that's the working set; the
 * toggle switches to /inquiries (everything, including closed and
 * converted) for history. The closure reason comes from the
 * closure_reasons master table — free text is deliberately not an option,
 * per the KB.
 */
export default function Inquiries() {
  const [params] = useSearchParams();
  const highlightId = Number(params.get("highlight")) || null;

  const staff = useSelector((state) => state.auth.staff);
  // A counselor's list is their own pipeline; Admin/Manager oversee the
  // whole institute, so they start unscoped. Either can flip it.
  const isOverseer = MASTER_DATA_ROLES.includes(staff?.role);
  const [mineOnly, setMineOnly] = useState(!isOverseer);

  const [showAll, setShowAll] = useState(false);
  const endpoint = showAll ? "/inquiries" : "/inquiries/active";

  const { data: inquiries, loading, error, fetchAll } = useApiResource(endpoint);
  const reasonsApi = useApiResource("/closure-reasons/active");

  const [search, setSearch] = useState("");
  const [closingRow, setClosingRow] = useState(null);
  const [closeStatus, setCloseStatus] = useState("Not Interested");
  const [reasonId, setReasonId] = useState("");
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState(null);
  const [converting, setConverting] = useState(null);
  // The row awaiting confirmation, or null. Holding the row rather than a
  // boolean means the dialog can name who it's about.
  const [confirmingConvert, setConfirmingConvert] = useState(null);
  const [rowError, setRowError] = useState(null);

  const refresh = () => fetchAll({ mine: mineOnly });

  useEffect(() => {
    refresh();
  }, [endpoint, mineOnly]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    reasonsApi.fetchAll();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const filtered = useMemo(() => {
    if (!search.trim()) return inquiries;
    const q = search.toLowerCase();
    return inquiries.filter((i) =>
      [i.enquirerName, i.email, i.phone, i.courseName, i.staffName, i.status]
        .some((v) => String(v ?? "").toLowerCase().includes(q))
    );
  }, [inquiries, search]);

  const openCloseForm = (row) => {
    setClosingRow(row);
    setCloseStatus("Not Interested");
    setReasonId("");
    setFormError(null);
  };

  const submitClose = async () => {
    if (!reasonId) {
      setFormError("A closure reason is required — pick one from the list.");
      return;
    }
    setSaving(true);
    setFormError(null);
    try {
      await axiosClient.put(`/inquiries/${closingRow.inquiryId}/close`, {
        status: closeStatus,
        closureReasonId: Number(reasonId),
      });
      setClosingRow(null);
      refresh();
    } catch (err) {
      setFormError(err.message);
    } finally {
      setSaving(false);
    }
  };

  /**
   * Marks the enquiry Converted. This is what Day 4's Student Registration
   * hangs off — students.inquiry_id is NOT NULL ("no enquiry, no
   * registration"), so an enquiry has to reach Converted before anyone can
   * be registered against it.
   */
  const convert = async () => {
    const row = confirmingConvert;
    if (!row) return;

    setConverting(row.inquiryId);
    setRowError(null);
    try {
      await axiosClient.put(`/inquiries/${row.inquiryId}/convert`);
      setConfirmingConvert(null);
      refresh();
    } catch (err) {
      setRowError(err.message);
      setConfirmingConvert(null);
    } finally {
      setConverting(null);
    }
  };

  /**
   * An open enquiry with no pending follow-up is invisible on the Follow-up
   * page — it happens when the enquiry arrived with no active counselor to
   * auto-assign to, or when a trail was ended without closing. This puts it
   * back on someone's list.
   */
  const scheduleFollowup = async (row) => {
    const suggested = new Date();
    suggested.setDate(suggested.getDate() + 3);
    const iso = suggested.toISOString().slice(0, 10);

    const date = window.prompt(
      `Schedule a follow-up for ${row.enquirerName} (YYYY-MM-DD):`,
      iso
    );
    if (!date) return;

    setRowError(null);
    try {
      // Falls back to the signed-in staff member when the enquiry has no
      // counselor yet — scheduling it also claims it.
      const ownerId = row.staffId ?? staff?.staffId;
      await axiosClient.post(
        `/followups?inquiryId=${row.inquiryId}&staffId=${ownerId}&date=${date}`
      );
      refresh();
    } catch (err) {
      setRowError(err.message);
    }
  };

  const isClosed = (status) => CLOSING_STATUSES.includes(status);

  return (
    <div className="inquiries-page">
      <div className="page-header">
        <h2>Enquiries</h2>
        <div className="page-header__controls">
          <input
            type="text"
            placeholder="Search enquiries..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <label className="inline-check">
            <input type="checkbox" checked={mineOnly} onChange={(e) => setMineOnly(e.target.checked)} />
            Only mine
          </label>
          <label className="inline-check">
            <input type="checkbox" checked={showAll} onChange={(e) => setShowAll(e.target.checked)} />
            Include closed &amp; converted
          </label>
        </div>
      </div>

      {loading && <p>Loading enquiries...</p>}
      {error && <p className="error">{error}</p>}
      {rowError && <p className="error">{rowError}</p>}

      <table>
        <thead>
          <tr>
            <th>#</th>
            <th>Enquirer</th>
            <th>Contact</th>
            <th>Course</th>
            <th>Source</th>
            <th>Counselor</th>
            <th>Enquired</th>
            <th>Next follow-up</th>
            <th>Status</th>
            <th>Action</th>
          </tr>
        </thead>
        <tbody>
          {filtered.map((i) => (
            <tr key={i.inquiryId} className={i.inquiryId === highlightId ? "row--highlight" : ""}>
              <td>{i.inquiryId}</td>
              <td>{i.enquirerName}</td>
              <td>
                {i.phone}
                <br />
                <small>{i.email}</small>
              </td>
              <td>{i.courseName}</td>
              <td>{i.source}</td>
              <td>{i.staffName || "—"}</td>
              <td>{i.inquiryDate}</td>
              <td>
                {i.nextFollowupDate || (
                  !isClosed(i.status) && i.status !== "Converted" ? (
                    <button
                      type="button"
                      className="link-button"
                      onClick={() => scheduleFollowup(i)}
                    >
                      Schedule
                    </button>
                  ) : "—"
                )}
              </td>
              <td>
                <span className={`badge badge--${String(i.status).toLowerCase().replace(/[^a-z]/g, "")}`}>
                  {i.status}
                </span>
                {i.closureReasonText && <small className="cell--reason">{i.closureReasonText}</small>}
              </td>
              <td className="cell--actions">
                {!isClosed(i.status) && i.status !== "Converted" ? (
                  <>
                    <button
                      type="button"
                      className="button--convert"
                      onClick={() => setConfirmingConvert(i)}
                      disabled={converting === i.inquiryId}
                    >
                      {converting === i.inquiryId ? "..." : "Convert"}
                    </button>
                    <button type="button" onClick={() => openCloseForm(i)}>Close</button>
                  </>
                ) : (
                  <span className="muted">—</span>
                )}
              </td>
            </tr>
          ))}
          {!loading && filtered.length === 0 && (
            <tr>
              <td colSpan={10}>No enquiries match.</td>
            </tr>
          )}
        </tbody>
      </table>

      {closingRow && (
        <div className="modal">
          <h3>Close enquiry #{closingRow.inquiryId} — {closingRow.enquirerName}</h3>

          {formError && <p className="error">{formError}</p>}

          <label>
            Outcome
            <select value={closeStatus} onChange={(e) => setCloseStatus(e.target.value)}>
              {CLOSING_STATUSES.map((s) => (
                <option key={s} value={s}>{s}</option>
              ))}
            </select>
          </label>

          <label>
            Reason *
            <select value={reasonId} onChange={(e) => setReasonId(e.target.value)}>
              <option value="">Select a reason...</option>
              {reasonsApi.data.map((r) => (
                <option key={r.reasonId} value={r.reasonId}>{r.reasonText}</option>
              ))}
            </select>
          </label>
          <p className="form-note">
            A reason is mandatory — a closed enquiry drops off the follow-up list, so this is the
            only record of why it ended.
          </p>

          <div className="modal__actions">
            <button type="button" onClick={submitClose} disabled={saving}>
              {saving ? "Closing..." : "Close enquiry"}
            </button>
            <button type="button" onClick={() => setClosingRow(null)}>Cancel</button>
          </div>
        </div>
      )}

      <ConfirmDialog
        open={Boolean(confirmingConvert)}
        title={`Convert enquiry #${confirmingConvert?.inquiryId ?? ""}`}
        message={`Mark ${confirmingConvert?.enquirerName ?? "this enquiry"} as Converted?`}
        detail="It leaves the follow-up list, and a student can then be registered against it."
        confirmLabel="Mark as Converted"
        busy={converting === confirmingConvert?.inquiryId}
        onConfirm={convert}
        onCancel={() => setConfirmingConvert(null)}
      />
    </div>
  );
}
