# ComputerSeekho — Project Knowledge Base
**Status:** Living document — updated as new source documents / decisions are received
**Last updated after:** Turn 5 — resolved the 3 open items from Turn 4 (email recipient, installment plan, address clarification)

---

## 0. Document Register

| # | Document | Type | Date | Author |
|---|----------|------|------|--------|
| 1 | `BRD.PDF` | Business Requirement Document v1.3 | 10-Oct-25 | Jayant Ponkshe |
| 2 | `updated_tb.txt` | Database Design (schema v3) | undated | — |
| 3 | `flow.txt` | Sample process flow + Frontend/Backend requirement checklists | undated | — |
| 4 | Turn 2 | Resolutions to Turn-1 conflicts & open items | — | You |
| 5 | Turn 3 | Table Maintenance scope, Excel upload target, recruiter FK unification, faculty-uniqueness enforcement, batch album UI | — | You |
| 6 | Turn 4 | Faculty's additional requirements — pre-meeting list + Ponkshe Sir's 2nd meeting (20-Jul-2026) | — | Faculty |
| 7 | Turn 5 | Resolved Turn-4's 3 open items | — | You |

---

## 1. Business Domain Summary
ComputerSeekho: public website + admin backend for SMVITA. Core pipeline: **Inquiry → Follow-up → Registration → Payment → Completion → Placement.** Backend: Spring Boot first, .NET planned later.

---

## 2. Core Business Process (updated)
```
Visitor fills Enquiry form (validated) → confirmation email fires to the
   enquirer (only — no separate counselor notification email)
   → Counselor auto-assigned (least-loaded round robin)
   → Follow-up default: 3 days after the enquiry itself
   → Staff attempts contact; if no response after ~3–4 days, enquiry MUST be
     closed with a mandatory reason (from a standard dropdown list)
   → Closed enquiries drop out of the active Enquiry/Follow-up lists
   → NO registration can happen without a referencing enquiry — this is now
     a hard rule, not just a guideline
   → Registration: staff picks Course from dropdown → fee auto-populates →
     fixed 2-installment breakdown shown → online (PDF) receipt generated
   → Student enrolled into a Batch; batch has a Batch Album (multiple
     photos, one designated cover photo)
   → On placement, student's recruiter drill-down works both ways: from
     "Our Recruiters" page, clicking a company logo shows all VITA
     students placed there
```

---

## 3. Functional Modules

### 3.1 Public Website — updates
| Page | New/Confirmed Detail |
|---|---|
| Enquiry Form | Needs field-level validation (required fields, email/phone format, etc.) and fires a confirmation email on submit — **recipient(s) not yet confirmed, see §8** |
| Our Recruiters | **New drill-down:** clicking a recruiter's logo opens a filtered view of VITA students placed at that company (query: `placement_records` filtered by `recruiter_id`, joined to `students` for photo/name) — this is exactly what last turn's `recruiter_id` FK unification was built for |
| Batch Album | **Already fully supported by existing schema** — clicking an album shows all its photos (`batch_album_images`), one photo is the designated cover (`batch_albums.cover_image`), one album per batch (already enforced via `UNIQUE(batch_id)`). Only refinement proposed: see §4.1. |

### 3.2 Admin Panel — updates
- **Add Enquiry:** now explicitly requires validation + triggers a **confirmation email to the enquirer only** (no separate counselor-notification email) on submission, any entry channel.
- **Follow-up:**
  - Default first follow-up date = **enquiry date + 3 days** (confirms/clarifies existing `next_followup` default rule).
  - If a student can't be reached after ~3–4 days, the enquiry **must** be closed, with a reason — reason is now **mandatory**, not optional, whenever an enquiry closes without conversion.
  - Once closed, an enquiry **no longer appears** in the active Enquiry/Follow-up lists.
- **Closing an enquiry:** must use a **standard dropdown of reasons** (not free text) — new master data, see §4.1.
- **Student Registration:**
  - Course field becomes a dropdown; selecting a course **auto-populates its fee** and **displays the installment breakdown**.
  - Installment plan is **fixed system-wide at 2 installments** (not staff-configurable) — `payments.total_installments` will consistently be `2`. Default assumption: 50/50 split of `courses.fees` unless told otherwise; the due date/timing for installment 2 isn't specified yet — flagging as a small implementation detail, not blocking.
  - Registration produces an **online receipt** (PDF — ties into the existing "Email and PDF wherever needed" backend requirement), not just a printed paper form.
  - **Hard rule confirmed:** a registration cannot exist without a referencing enquiry — schema now enforces this, see §4.1.

---

## 4. Data Model

### 4.1 Schema changes this turn

| Table | Change | Reason |
|---|---|---|
| `students` | `inquiry_id` changes from **nullable** → **`NOT NULL`**; FK behavior changes from `ON DELETE SET NULL` → `ON DELETE RESTRICT` | "Without enquiry, no registration will happen" — now a hard DB rule, not just a UI convention. `RESTRICT` protects against deleting an inquiry that already produced a student. |
| `enrollments` | `inquiry_id` changes from **nullable** → **`NOT NULL`**, same FK behavior change | Every enrollment (even a repeat student enrolling in a second course) must trace to *some* enquiry, per BRD's original Course-A/Course-B example — now enforced, not just described. |
| **`closure_reasons`** *(new table)* | `reason_id`, `reason_text` (unique), `is_active` | Backs the new standard-reasons dropdown for closing an enquiry |
| `inquiries` | **Added** `closure_reason_id INT NULL`, FK → `closure_reasons.reason_id`, `ON DELETE RESTRICT` | Captures why an enquiry was closed. App-level rule: **mandatory** whenever `status` is set to `Lost` or `Not Interested`; not required for `Converted` (that's a successful outcome, not a "closure with reason") |
| `batch_albums` | **Proposed:** change `cover_image VARCHAR(500)` → `cover_image_id INT NULL` FK → `batch_album_images.image_id` (`ON DELETE SET NULL`) | Faculty's note — *"any 1 photo will be the cover photo"* — implies the cover is picked **from** the album's own photos, not uploaded separately. A raw duplicate URL risks drifting out of sync; referencing the actual image row keeps it consistent. **Flagging as my recommendation, not yet confirmed** — easy to keep the current raw-URL field instead if you'd rather allow a distinct cover image. |

### 4.2 Confirmed already-satisfied (no change needed)
- **Address segregation for `students`** — already has `address_line1`, `address_line2`, `city`, `state`, `pincode`. ✅ **Confirmed this is the student's own address only — no separate Office Address is needed** (Annexure 1's paper-form "Office Address" block is not being carried into the online system).
- **Batch albums (multiple images, one cover, batch-wise)** — already fully modeled via `batch_albums` + `batch_album_images`. ✅

### 4.3 Table Maintenance master list — add `closure_reasons`
Extending Turn 3's confirmed list (`course_categories`, `courses`, `recruiters`, `announcements`, `staff`, `banners`, `news_events`, `testimonials`, `gallery_images`, `placement_drives`, `placement_records`) with **`closure_reasons`** — it's pure lookup/master data and needs the same add/update/delete grid.

---

## 5. Frontend / 6. Backend Requirements
Unchanged. Note: this turn's receipt-generation and enquiry-confirmation-email items are concrete instances of the previously abstract "Email and PDF wherever needed" backend requirement.

---

## 7 / 8. Conflicts & Open Items

**No hard conflicts.** All 3 items open at the end of Turn 4 are now resolved:

| # | Item | Resolution |
|---|---|---|
| 1 | Enquiry confirmation email recipient | **Enquirer only** — no separate counselor-notification email |
| 2 | Installment plan | **Fixed at 2 installments**, system-wide, not staff-configurable |
| 3 | Office Address | **Not needed** — the segregated address on `students` is the student's own address; Annexure 1's "Office Address" block isn't being carried into the online system |

**One small residual detail, not blocking:** the due date/timing rule for the 2nd installment (e.g., a fixed number of days after registration, before batch completion, etc.) hasn't been specified. Will proceed with the field structure as-is (`payments.installment_number`/`total_installments`) and flag this only if it becomes relevant when building the actual payment-scheduling logic.

---

## 9. Architecture Decisions Log
*(Turn 1–3 entries preserved below; nothing added this turn beyond the schema items in §4.1, which are pending your confirmation rather than fully closed decisions.)*

**9.1** Table Maintenance scope + dedicated Batch Management screen (`batches` excluded from generic grid; hosts Batch Album management)
**9.2** Excel upload primary target = `students`
**9.3** Recruiter identity unified via `recruiter_id` FK on `placement_drives`/`placement_records`
**9.4** `course_staff` primary-faculty uniqueness enforced at the Service layer (unset-then-set in one transaction)
**9.5** Batch Album lives inside the Batch Management screen
**9.6** Counselor auto-assignment = least-loaded round robin (fewest open inquiries among active Counselors)

---

## 10. Change Log
| Turn | Change |
|---|---|
| 1 | Initial KB. 3 conflicts + 12 open items logged. |
| 2 | Resolved 3 conflicts + 10/12 open items. Fee-date columns removed; `announcements` + `recruiters` tables added; `course_staff.is_primary` formalized; About page + Online-PG-Diploma nav excluded; counselor auto-assignment decided. |
| 3 | Table Maintenance scope + dedicated Batch Management screen decided; Excel upload target = Students; recruiter identity unified via FK; `course_staff` uniqueness enforcement decided; Batch Album UI placement confirmed. |
| 4 | Faculty's additional requirements incorporated: enquiry form validation + email trigger; mandatory closure reason + standard reasons dropdown (**new `closure_reasons` table**); enquiry no longer required optional — **`students.inquiry_id` and `enrollments.inquiry_id` now `NOT NULL`**; closed enquiries hidden from active lists; Registration course-dropdown auto-fee + installment display + online PDF receipt; recruiter drill-down page (validates Turn 3's `recruiter_id` FK decision); confirmed address segregation and batch album structure already satisfied, with one proposed refinement to `batch_albums.cover_image`. 3 new open items logged (§8). |
| 5 | Resolved all 3 Turn-4 open items: enquiry confirmation email goes to the enquirer only; installment plan fixed at 2 installments system-wide; confirmed `students` address fields represent the student's own address only (no separate Office Address needed). No open items remain. |
