import { useEffect } from "react";

/**
 * Replaces window.confirm for destructive and irreversible actions.
 *
 * The native dialog is styled by the browser, renders at the top of the
 * screen detached from what you clicked, and on Chrome carries a "prevent
 * this page from creating more dialogs" checkbox that can disable
 * confirmation for the rest of the session — silently turning a guarded
 * action into a one-click one.
 *
 * It also can't show the consequence properly. "Mark enquiry #12 as
 * Converted?" is a yes/no; what the user needs to know is that the enquiry
 * leaves the follow-up list and becomes registerable, which is what the
 * `detail` slot is for.
 */
export default function ConfirmDialog({
  open,
  title,
  message,
  detail,
  confirmLabel = "Confirm",
  cancelLabel = "Cancel",
  busy = false,
  onConfirm,
  onCancel,
}) {
  // Escape closes it. Native confirm() gives you this for free and losing
  // it is the kind of regression that makes a custom dialog feel worse
  // than the thing it replaced.
  useEffect(() => {
    if (!open) return undefined;
    const onKey = (e) => {
      if (e.key === "Escape" && !busy) onCancel();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, busy, onCancel]);

  if (!open) return null;

  return (
    <div
      className="modal"
      role="dialog"
      aria-modal="true"
      // Clicking the backdrop cancels, but only the backdrop itself —
      // without the target check, a click that starts inside the panel and
      // drifts outward would close a half-filled dialog.
      onMouseDown={(e) => {
        if (e.target === e.currentTarget && !busy) onCancel();
      }}
    >
      <h3>{title}</h3>
      <p>{message}</p>
      {detail && <p className="modal__meta">{detail}</p>}

      <div className="modal__actions">
        <button type="button" onClick={onConfirm} disabled={busy} autoFocus>
          {busy ? "Working..." : confirmLabel}
        </button>
        <button type="button" onClick={onCancel} disabled={busy}>
          {cancelLabel}
        </button>
      </div>
    </div>
  );
}
