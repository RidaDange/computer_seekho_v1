import { useEffect, useState } from "react";
import axiosClient from "../../api/axiosClient";
import ConfirmDialog from "../../components/common/ConfirmDialog";
import useApiResource from "../../hooks/useApiResource";

const MODES = ["Offline", "Online", "Hybrid"];
const DRIVE_STATUSES = ["Scheduled", "Completed", "Cancelled"];

const money = (v) =>
  v == null ? "—" : `₹${Number(v).toLocaleString("en-IN")}`;

/**
 * Placement entry — drives and their outcomes, on one screen with two tabs.
 *
 * They're separate tables for a reason: a drive is the recruiter's visit
 * (TCS came on the 12th, 40 openings) and a record is an individual result
 * (Rahul got one). Keeping them adjacent means you can log the drive and
 * then its placements without navigating away.
 */
export default function PlacementEntry() {
  const [tab, setTab] = useState("records");

  const drivesApi = useApiResource("/placement-drives");
  const recordsApi = useApiResource("/placement-records");
  const recruitersApi = useApiResource("/recruiters");
  const studentsApi = useApiResource("/students");
  const coursesApi = useApiResource("/courses");

  const [driveDraft, setDriveDraft] = useState(null);
  const [recordDraft, setRecordDraft] = useState(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [confirmingDelete, setConfirmingDelete] = useState(null);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    drivesApi.fetchAll();
    recordsApi.fetchAll();
    recruitersApi.fetchAll();
    studentsApi.fetchAll();
    coursesApi.fetchAll();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const saveDrive = async () => {
    setSaving(true);
    setError(null);
    try {
      const payload = {
        ...driveDraft,
        recruiterId: Number(driveDraft.recruiterId),
        courseId: driveDraft.courseId ? Number(driveDraft.courseId) : null,
        noOfOpenings: driveDraft.noOfOpenings ? Number(driveDraft.noOfOpenings) : null,
        noOfStudentsSelected: driveDraft.noOfStudentsSelected
          ? Number(driveDraft.noOfStudentsSelected) : null,
        packageAmount: driveDraft.packageAmount ? Number(driveDraft.packageAmount) : null,
      };
      if (driveDraft.driveId) {
        await axiosClient.put(`/placement-drives/${driveDraft.driveId}`, payload);
      } else {
        await axiosClient.post("/placement-drives", payload);
      }
      setDriveDraft(null);
      drivesApi.fetchAll();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const saveRecord = async () => {
    setSaving(true);
    setError(null);
    try {
      const payload = {
        ...recordDraft,
        studentId: Number(recordDraft.studentId),
        recruiterId: Number(recordDraft.recruiterId),
        // Left null, the server defaults it from the student's enrolment —
        // a placement with no batch drops off the Batchwise Placement page.
        batchId: recordDraft.batchId ? Number(recordDraft.batchId) : null,
        packageAmount: recordDraft.packageAmount ? Number(recordDraft.packageAmount) : null,
        isFeatured: Boolean(recordDraft.isFeatured),
      };
      if (recordDraft.placementId) {
        await axiosClient.put(`/placement-records/${recordDraft.placementId}`, payload);
      } else {
        await axiosClient.post("/placement-records", payload);
      }
      setRecordDraft(null);
      recordsApi.fetchAll();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const removeRecord = async () => {
    setDeleting(true);
    try {
      await axiosClient.delete(`/placement-records/${confirmingDelete.placementId}`);
      recordsApi.fetchAll();
    } catch (err) {
      setError(err.message);
    } finally {
      setDeleting(false);
      setConfirmingDelete(null);
    }
  };

  return (
    <div className="placement-page">
      <div className="page-header">
        <h2>Placements</h2>
        <div className="page-header__controls">
          <button
            type="button"
            className="button"
            onClick={() =>
              tab === "records"
                ? setRecordDraft({ placementDate: new Date().toISOString().slice(0, 10) })
                : setDriveDraft({ driveMode: "Offline", driveStatus: "Scheduled" })
            }
          >
            + {tab === "records" ? "Placement" : "Drive"}
          </button>
        </div>
      </div>

      <div className="batch-panel__tabs">
        <button type="button" className={tab === "records" ? "is-active" : ""} onClick={() => setTab("records")}>
          Placed students ({recordsApi.data.length})
        </button>
        <button type="button" className={tab === "drives" ? "is-active" : ""} onClick={() => setTab("drives")}>
          Drives ({drivesApi.data.length})
        </button>
      </div>

      {error && <p className="error">{error}</p>}

      {tab === "records" && (
        <table>
          <thead>
            <tr>
              <th>Student</th><th>Company</th><th>Position</th>
              <th>Package</th><th>Batch</th><th>Date</th><th>Featured</th><th></th>
            </tr>
          </thead>
          <tbody>
            {recordsApi.data.map((r) => (
              <tr key={r.placementId}>
                <td>{r.studentName}</td>
                <td>{r.recruiterCompanyName}</td>
                <td>{r.position || "—"}</td>
                <td>{money(r.packageAmount)}</td>
                <td>{r.batchName || "—"}</td>
                <td>{r.placementDate || "—"}</td>
                <td>{r.isFeatured ? "Yes" : "—"}</td>
                <td className="cell--actions">
                  <button type="button" onClick={() => setRecordDraft({ ...r })}>Edit</button>
                  <button type="button" onClick={() => setConfirmingDelete(r)}>Delete</button>
                </td>
              </tr>
            ))}
            {recordsApi.data.length === 0 && (
              <tr><td colSpan={8}>No placements recorded yet.</td></tr>
            )}
          </tbody>
        </table>
      )}

      {tab === "drives" && (
        <table>
          <thead>
            <tr>
              <th>Date</th><th>Company</th><th>Position</th><th>Course</th>
              <th>Mode</th><th>Openings</th><th>Selected</th><th>Status</th><th></th>
            </tr>
          </thead>
          <tbody>
            {drivesApi.data.map((d) => (
              <tr key={d.driveId}>
                <td>{d.driveDate}</td>
                <td>{d.recruiterCompanyName}</td>
                <td>{d.position}</td>
                <td>{d.courseName || "Open"}</td>
                <td>{d.driveMode}</td>
                <td>{d.noOfOpenings ?? "—"}</td>
                <td>{d.noOfStudentsSelected ?? "—"}</td>
                <td><span className="badge">{d.driveStatus}</span></td>
                <td><button type="button" onClick={() => setDriveDraft({ ...d })}>Edit</button></td>
              </tr>
            ))}
            {drivesApi.data.length === 0 && (
              <tr><td colSpan={9}>No drives logged yet.</td></tr>
            )}
          </tbody>
        </table>
      )}

      {recordDraft && (
        <div className="modal">
          <h3>{recordDraft.placementId ? "Edit" : "New"} placement</h3>
          <label>
            Student
            <select
              value={recordDraft.studentId || ""}
              onChange={(e) => setRecordDraft({ ...recordDraft, studentId: e.target.value })}
            >
              <option value="">Select...</option>
              {studentsApi.data.map((s) => (
                <option key={s.studentId} value={s.studentId}>
                  {s.firstName} {s.lastName}{s.batchName ? ` — ${s.batchName}` : ""}
                </option>
              ))}
            </select>
          </label>
          <label>
            Company
            <select
              value={recordDraft.recruiterId || ""}
              onChange={(e) => setRecordDraft({ ...recordDraft, recruiterId: e.target.value })}
            >
              <option value="">Select...</option>
              {recruitersApi.data.map((r) => (
                <option key={r.recruiterId} value={r.recruiterId}>{r.companyName}</option>
              ))}
            </select>
          </label>
          <label>
            Position
            <input
              value={recordDraft.position || ""}
              onChange={(e) => setRecordDraft({ ...recordDraft, position: e.target.value })}
            />
          </label>
          <label>
            Package (annual CTC)
            <input
              type="number"
              value={recordDraft.packageAmount ?? ""}
              onChange={(e) => setRecordDraft({ ...recordDraft, packageAmount: e.target.value })}
            />
          </label>
          <label>
            Placement date
            <input
              type="date"
              value={recordDraft.placementDate || ""}
              onChange={(e) => setRecordDraft({ ...recordDraft, placementDate: e.target.value })}
            />
          </label>
          <label className="inline-check">
            <input
              type="checkbox"
              checked={Boolean(recordDraft.isFeatured)}
              onChange={(e) => setRecordDraft({ ...recordDraft, isFeatured: e.target.checked })}
            />
            Feature on the public site
          </label>

          <div className="modal__actions">
            <button type="button" onClick={saveRecord} disabled={saving}>
              {saving ? "Saving..." : "Save"}
            </button>
            <button type="button" onClick={() => setRecordDraft(null)}>Cancel</button>
          </div>
        </div>
      )}

      {driveDraft && (
        <div className="modal">
          <h3>{driveDraft.driveId ? "Edit" : "New"} drive</h3>
          <label>
            Company
            <select
              value={driveDraft.recruiterId || ""}
              onChange={(e) => setDriveDraft({ ...driveDraft, recruiterId: e.target.value })}
            >
              <option value="">Select...</option>
              {recruitersApi.data.map((r) => (
                <option key={r.recruiterId} value={r.recruiterId}>{r.companyName}</option>
              ))}
            </select>
          </label>
          <label>
            Course (leave blank for an open drive)
            <select
              value={driveDraft.courseId || ""}
              onChange={(e) => setDriveDraft({ ...driveDraft, courseId: e.target.value })}
            >
              <option value="">Open to all</option>
              {coursesApi.data.map((c) => (
                <option key={c.courseId} value={c.courseId}>{c.name}</option>
              ))}
            </select>
          </label>
          <label>
            Drive date
            <input
              type="date"
              value={driveDraft.driveDate || ""}
              onChange={(e) => setDriveDraft({ ...driveDraft, driveDate: e.target.value })}
            />
          </label>
          <label>
            Position
            <input
              value={driveDraft.position || ""}
              onChange={(e) => setDriveDraft({ ...driveDraft, position: e.target.value })}
            />
          </label>
          <label>
            Mode
            <select
              value={driveDraft.driveMode || "Offline"}
              onChange={(e) => setDriveDraft({ ...driveDraft, driveMode: e.target.value })}
            >
              {MODES.map((m) => <option key={m} value={m}>{m}</option>)}
            </select>
          </label>
          <label>
            Package (annual CTC)
            <input
              type="number"
              value={driveDraft.packageAmount ?? ""}
              onChange={(e) => setDriveDraft({ ...driveDraft, packageAmount: e.target.value })}
            />
          </label>
          <label>
            Openings
            <input
              type="number"
              value={driveDraft.noOfOpenings ?? ""}
              onChange={(e) => setDriveDraft({ ...driveDraft, noOfOpenings: e.target.value })}
            />
          </label>
          <label>
            Students selected
            <input
              type="number"
              value={driveDraft.noOfStudentsSelected ?? ""}
              onChange={(e) => setDriveDraft({ ...driveDraft, noOfStudentsSelected: e.target.value })}
            />
          </label>
          <label>
            Status
            <select
              value={driveDraft.driveStatus || "Scheduled"}
              onChange={(e) => setDriveDraft({ ...driveDraft, driveStatus: e.target.value })}
            >
              {DRIVE_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
            </select>
          </label>
          <label>
            Eligibility
            <textarea
              value={driveDraft.eligibilityCriteria || ""}
              onChange={(e) => setDriveDraft({ ...driveDraft, eligibilityCriteria: e.target.value })}
            />
          </label>

          <div className="modal__actions">
            <button type="button" onClick={saveDrive} disabled={saving}>
              {saving ? "Saving..." : "Save"}
            </button>
            <button type="button" onClick={() => setDriveDraft(null)}>Cancel</button>
          </div>
        </div>
      )}

      <ConfirmDialog
        open={Boolean(confirmingDelete)}
        title="Remove placement record"
        message={`Remove ${confirmingDelete?.studentName ?? "this student"}'s placement at ${
          confirmingDelete?.recruiterCompanyName ?? "this company"
        }?`}
        detail="The public Batchwise Placement figures will change to match."
        confirmLabel="Remove"
        busy={deleting}
        onConfirm={removeRecord}
        onCancel={() => setConfirmingDelete(null)}
      />
    </div>
  );
}
