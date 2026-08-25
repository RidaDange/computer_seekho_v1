import { Fragment, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import axiosClient from "../../api/axiosClient";
import useApiResource from "../../hooks/useApiResource";
import openReceipt from "../../utils/openReceipt";

const PAYMENT_MODES = ["Cash", "UPI", "Card", "Bank Transfer", "Cheque"];

const money = (v) =>
  v == null ? "—" : `₹${Number(v).toLocaleString("en-IN", { minimumFractionDigits: 2 })}`;

/**
 * Registered students, their fee position, and their receipts.
 *
 * Doubles as the collection screen for installment 2 — the outstanding
 * balance is where a counsellor naturally goes looking for it, rather than
 * a separate "Payments" nav item that would only ever hold one action.
 */
export default function Students() {
  const { data: students, loading, error, fetchAll } = useApiResource("/students");

  const [search, setSearch] = useState("");
  const [expanded, setExpanded] = useState(null); // studentId whose payments are open
  const [payments, setPayments] = useState([]);
  const [fees, setFees] = useState(null);
  const [collecting, setCollecting] = useState(null); // student being collected from
  const [form, setForm] = useState({ paymentMode: "Cash" });
  const [saving, setSaving] = useState(false);
  const [rowError, setRowError] = useState(null);

  useEffect(() => {
    fetchAll();
  }, [fetchAll]);

  const filtered = useMemo(() => {
    if (!search.trim()) return students;
    const q = search.toLowerCase();
    return students.filter((s) =>
      [s.firstName, s.lastName, s.email, s.phone, s.batchName, s.courseName]
        .some((v) => String(v ?? "").toLowerCase().includes(q))
    );
  }, [students, search]);

  const openDetail = async (student) => {
    if (expanded === student.studentId) {
      setExpanded(null);
      return;
    }
    setRowError(null);
    setExpanded(student.studentId);
    try {
      const [payRes, feeRes] = await Promise.all([
        axiosClient.get(`/students/${student.studentId}/payments`),
        student.enrollmentId
          ? axiosClient.get(`/payments/enrollments/${student.enrollmentId}/fees`)
          : Promise.resolve({ data: null }),
      ]);
      setPayments(payRes.data);
      setFees(feeRes.data);
    } catch (err) {
      setRowError(err.message);
    }
  };

  const outstanding = useMemo(() => {
    if (!fees) return null;
    const paid = payments.reduce((sum, p) => sum + Number(p.amount), 0);
    return Number(fees.totalFees) - paid;
  }, [fees, payments]);

  const openCollect = (student) => {
    setCollecting(student);
    setForm({ paymentMode: "Cash", amount: outstanding ?? "" });
    setRowError(null);
  };

  const submitCollect = async () => {
    setSaving(true);
    setRowError(null);
    try {
      await axiosClient.post("/payments", {
        enrollmentId: collecting.enrollmentId,
        amount: Number(form.amount),
        paymentMode: form.paymentMode,
        transactionId: form.transactionId || null,
        remarks: form.remarks || null,
      });
      setCollecting(null);
      await openDetail({ studentId: -1 });      // collapse
      await openDetail(collecting);             // and reload
    } catch (err) {
      setRowError(err.message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="students-page">
      <div className="page-header">
        <h2>Students</h2>
        <div className="page-header__controls">
          <input
            type="text"
            placeholder="Search students..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <Link to="/admin/registration" className="button">+ Register Student</Link>
        </div>
      </div>

      {loading && <p>Loading students...</p>}
      {error && <p className="error">{error}</p>}
      {rowError && <p className="error">{rowError}</p>}

      <table>
        <thead>
          <tr>
            <th>#</th><th>Name</th><th>Contact</th><th>Course</th><th>Batch</th>
            <th>Registered</th><th>Enquiry</th><th></th>
          </tr>
        </thead>
        <tbody>
          {filtered.map((s) => (
            <Fragment key={s.studentId}>
              <tr>
                <td>{s.studentId}</td>
                <td>{s.firstName} {s.lastName}</td>
                <td>{s.phone}<br /><small>{s.email}</small></td>
                <td>{s.courseName || "—"}</td>
                <td>{s.batchName || "—"}</td>
                <td>{s.regDate}</td>
                <td>
                  <Link to={`/admin/inquiries?highlight=${s.inquiryId}`}>#{s.inquiryId}</Link>
                </td>
                <td>
                  <button type="button" onClick={() => openDetail(s)}>
                    {expanded === s.studentId ? "Hide fees" : "Fees & receipts"}
                  </button>
                </td>
              </tr>

              {expanded === s.studentId && (
                <tr className="row--detail">
                  <td colSpan={8}>
                    {/* The due-date clause is conditional. A student who
                        paid in full still has an installment 2 in the plan,
                        but printing "due 2026-09-02" next to "Fully paid"
                        reads as a contradiction. */}
                    {fees && (
                      <p>
                        Total {money(fees.totalFees)} ·{" "}
                        {outstanding > 0 ? (
                          <>
                            <strong>{money(outstanding)} outstanding</strong>, due{" "}
                            {fees.installment2DueDate}
                          </>
                        ) : (
                          <strong>Fully paid</strong>
                        )}
                      </p>
                    )}

                    <table className="table--nested">
                      <thead>
                        <tr>
                          <th>Installment</th><th>Receipt</th><th>Date</th>
                          <th>Amount</th><th>Mode</th><th>Status</th><th></th>
                        </tr>
                      </thead>
                      <tbody>
                        {payments.map((p) => (
                          <tr key={p.paymentId}>
                            <td>{p.installmentNumber} of {p.totalInstallments}</td>
                            <td>{p.receiptNo}</td>
                            <td>{p.paymentDate}</td>
                            <td>{money(p.amount)}</td>
                            <td>{p.paymentMode}</td>
                            <td>{p.paymentStatus}</td>
                            <td>
                              <button
                                type="button"
                                className="link-button"
                                onClick={() =>
                                  openReceipt(p.paymentId).catch((err) => setRowError(err.message))
                                }
                              >
                                Receipt PDF
                              </button>
                            </td>
                          </tr>
                        ))}
                        {payments.length === 0 && (
                          <tr><td colSpan={7}>No payments recorded.</td></tr>
                        )}
                      </tbody>
                    </table>

                    {outstanding > 0 && s.enrollmentId && (
                      <button type="button" className="button" onClick={() => openCollect(s)}>
                        Collect {money(outstanding)}
                      </button>
                    )}
                  </td>
                </tr>
              )}
            </Fragment>
          ))}
          {!loading && filtered.length === 0 && (
            <tr><td colSpan={8}>No students yet. Register one from an enquiry.</td></tr>
          )}
        </tbody>
      </table>

      {collecting && (
        <div className="modal">
          <h3>Collect payment — {collecting.firstName} {collecting.lastName}</h3>
          <p className="modal__meta">{collecting.courseName} · {collecting.batchName}</p>

          {rowError && <p className="error">{rowError}</p>}

          <label>
            Amount
            <input
              type="number"
              step="0.01"
              value={form.amount ?? ""}
              onChange={(e) => setForm({ ...form, amount: e.target.value })}
            />
          </label>
          <label>
            Mode
            <select
              value={form.paymentMode}
              onChange={(e) => setForm({ ...form, paymentMode: e.target.value })}
            >
              {PAYMENT_MODES.map((m) => <option key={m} value={m}>{m}</option>)}
            </select>
          </label>
          <label>
            Transaction ref
            <input
              value={form.transactionId || ""}
              onChange={(e) => setForm({ ...form, transactionId: e.target.value })}
            />
          </label>

          <div className="modal__actions">
            <button type="button" onClick={submitCollect} disabled={saving}>
              {saving ? "Saving..." : "Record payment"}
            </button>
            <button type="button" onClick={() => setCollecting(null)}>Cancel</button>
          </div>
        </div>
      )}
    </div>
  );
}
