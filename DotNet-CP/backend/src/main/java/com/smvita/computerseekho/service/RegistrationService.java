package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.*;
import com.smvita.computerseekho.entity.*;
import com.smvita.computerseekho.exception.BusinessRuleException;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.*;
import lombok.RequiredArgsConstructor;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.List;
import java.util.Set;

/**
 * Student Registration — the lead-to-student conversion.
 *
 * Everything happens in one transaction: student, enrolment, first payment,
 * the batch's headcount, and the enquiry's status. Splitting them would
 * allow a student with no enrolment, or an enrolment nobody paid for, and
 * there'd be no natural point at which to notice or clean that up.
 *
 * Hard rules from the Knowledge Base:
 *   §4.1  no enquiry, no registration — and one enquiry yields one student
 *   §3.2  course fee auto-populates; fixed 2-installment split
 *   §3.2  an online (PDF) receipt is produced, not just a paper form
 */
@Service
@RequiredArgsConstructor
@Transactional
public class RegistrationService {

    private static final Logger log = LoggerFactory.getLogger(RegistrationService.class);

    /** Batches you can still join. A Completed or Cancelled batch can't take new students. */
    private static final Set<Batch.Status> JOINABLE_STATUSES =
            Set.of(Batch.Status.Upcoming, Batch.Status.Ongoing);

    private final StudentRepository studentRepository;
    private final EnrollmentRepository enrollmentRepository;
    private final PaymentRepository paymentRepository;
    private final InquiryRepository inquiryRepository;
    private final BatchRepository batchRepository;
    private final CourseRepository courseRepository;
    private final FeeCalculator feeCalculator;
    private final ReceiptPdfService receiptPdfService;
    private final EmailService emailService;

    // ------------------------------------------------------------ lookups

    /**
     * Step 1 of the wizard: find the enquiry to register against. Only
     * enquiries that haven't already produced a student are eligible —
     * showing the rest would just be offering a click that always fails.
     */
    @Transactional(readOnly = true)
    public List<InquiryDto> searchRegisterableInquiries(String query) {
        String q = query == null ? "" : query.trim().toLowerCase();

        return inquiryRepository.findAll().stream()
                .filter(i -> i.getStatus() != Inquiry.Status.Lost
                        && i.getStatus() != Inquiry.Status.NotInterested)
                .filter(i -> !studentRepository.existsByInquiry_InquiryId(i.getInquiryId()))
                .filter(i -> q.isEmpty()
                        || String.valueOf(i.getInquiryId()).contains(q)
                        || contains(i.getEnquirerName(), q)
                        || contains(i.getEmail(), q)
                        || contains(i.getPhone(), q))
                .map(this::toInquirySummary)
                .toList();
    }

    /**
     * Batches a given enquiry can be registered into — the enquiry's own
     * course, still joinable. Retained as the wizard's default; once the
     * counsellor changes the course dropdown the by-course variant takes
     * over.
     */
    @Transactional(readOnly = true)
    public List<BatchDto> joinableBatchesFor(Integer inquiryId) {
        Inquiry inquiry = getInquiryOrThrow(inquiryId);
        return joinableBatchesForCourse(inquiry.getCourse().getCourseId());
    }

    /**
     * Batches for an explicitly chosen course. The course dropdown on step 3
     * repoints the batch list through here, so the two can never drift apart
     * — every batch offered is one the selected course actually runs.
     */
    @Transactional(readOnly = true)
    public List<BatchDto> joinableBatchesForCourse(Integer courseId) {
        getCourseOrThrow(courseId);
        return batchRepository
                .findByCourse_CourseIdAndIsActiveTrueAndStatusInOrderByStartDateAsc(
                        courseId, JOINABLE_STATUSES)
                .stream().map(this::toBatchSummary).toList();
    }

    /** The fee panel on step 3 — auto-populated from the enquiry's course. */
    @Transactional(readOnly = true)
    public FeeBreakdownDto feeBreakdownFor(Integer inquiryId) {
        Inquiry inquiry = getInquiryOrThrow(inquiryId);
        return feeCalculator.breakdownFor(inquiry.getCourse(), LocalDate.now());
    }

    /**
     * The fee panel for an explicitly chosen course. Changing the course
     * changes what's owed, so the panel has to recompute — otherwise the
     * counsellor would collect installment 1 of the course the prospect
     * asked about rather than the one they're enrolling in.
     */
    @Transactional(readOnly = true)
    public FeeBreakdownDto feeBreakdownForCourse(Integer courseId) {
        return feeCalculator.breakdownFor(getCourseOrThrow(courseId), LocalDate.now());
    }

    // ------------------------------------------------------------- writes

    public RegistrationResult register(RegistrationRequest request) {
        Inquiry inquiry = getInquiryOrThrow(request.inquiryId());

        if (inquiry.getStatus() == Inquiry.Status.Lost
                || inquiry.getStatus() == Inquiry.Status.NotInterested) {
            throw new BusinessRuleException(
                    "Enquiry #" + inquiry.getInquiryId() + " is closed and can't be registered.");
        }
        if (studentRepository.existsByInquiry_InquiryId(inquiry.getInquiryId())) {
            throw new BusinessRuleException(
                    "Enquiry #" + inquiry.getInquiryId() + " has already been registered.");
        }

        StudentDetailsRequest details = request.student();
        if (studentRepository.existsByEmailIgnoreCase(details.email().trim())) {
            throw new BusinessRuleException(
                    "A student is already registered with the email " + details.email() + ".");
        }

        Batch batch = batchRepository.findById(request.batchId())
                .orElseThrow(() -> new ResourceNotFoundException("Batch", request.batchId()));

        // What they're actually signing up for. Defaults to the enquiry's
        // course; the step-3 dropdown can override it, because a prospect
        // who enquired about one course and enrolled in another is ordinary
        // front-desk reality, not a data error.
        Course course = resolveCourse(request.courseId(), inquiry);

        // The batch still has to belong to that course. Relaxing "batch must
        // match the enquiry" did not relax this: if the batch and the course
        // disagree, the fee charged and the course attended diverge, which is
        // the failure the original rule existed to prevent.
        if (!batch.getCourse().getCourseId().equals(course.getCourseId())) {
            throw new BusinessRuleException("%s runs %s, but you selected %s."
                    .formatted(batch.getBatchName(), batch.getCourse().getName(),
                            course.getName()));
        }
        if (!JOINABLE_STATUSES.contains(batch.getStatus())) {
            throw new BusinessRuleException(
                    batch.getBatchName() + " is " + batch.getStatus() + " and isn't taking new students.");
        }

        long occupied = enrollmentRepository.countByBatch_BatchIdAndStatusNot(
                batch.getBatchId(), Enrollment.Status.Dropped);
        if (occupied >= batch.getCapacity()) {
            throw new BusinessRuleException("%s is full (%d/%d)."
                    .formatted(batch.getBatchName(), occupied, batch.getCapacity()));
        }

        LocalDate today = LocalDate.now();
        FeeBreakdownDto fees = feeCalculator.breakdownFor(course, today);

        // The enquiry is corrected to the course actually taken. Leaving it
        // pointing at the original would make every course-wise enquiry
        // report disagree with the enrolment figures, and `students` reaches
        // its course through the enrolment's batch either way — so the stale
        // value would inform nothing and mislead anything that read it.
        if (!course.getCourseId().equals(inquiry.getCourse().getCourseId())) {
            log.info("Enquiry #{} switched from {} to {} at registration",
                    inquiry.getInquiryId(), inquiry.getCourse().getName(), course.getName());
            inquiry.setCourse(course);
        }

        Student student = buildStudent(details, inquiry, today);
        student = studentRepository.save(student);

        Enrollment enrollment = new Enrollment();
        enrollment.setStudent(student);
        enrollment.setBatch(batch);
        enrollment.setInquiry(inquiry);
        enrollment.setEnrollDate(today);
        enrollment.setStatus(Enrollment.Status.Active);
        enrollment = enrollmentRepository.save(enrollment);

        Payment firstPayment = recordFirstInstallment(request, student, enrollment, fees, today);

        // Recalculated, never incremented: the column is documented as
        // system-calculated, and a counter that drifts once stays wrong.
        batch.setCurrentCount((int) enrollmentRepository.countByBatch_BatchIdAndStatusNot(
                batch.getBatchId(), Enrollment.Status.Dropped));
        batchRepository.save(batch);

        inquiry.setStatus(Inquiry.Status.Converted);
        inquiry.setClosureReason(null);
        inquiryRepository.save(inquiry);

        boolean emailed = emailReceipt(student, enrollment, firstPayment, fees);

        log.info("Registered student #{} ({}) into batch '{}' against enquiry #{}",
                student.getStudentId(), student.getEmail(), batch.getBatchName(), inquiry.getInquiryId());

        return new RegistrationResult(
                toStudentDto(student, enrollment),
                toPaymentDto(firstPayment),
                fees,
                "/payments/" + firstPayment.getPaymentId() + "/receipt",
                emailed
        );
    }

    // ------------------------------------------------------------ helpers

    private Payment recordFirstInstallment(RegistrationRequest request, Student student,
                                           Enrollment enrollment, FeeBreakdownDto fees, LocalDate today) {
        BigDecimal expected = fees.installment1Amount();
        BigDecimal amount = request.amountPaid() != null ? request.amountPaid() : expected;

        // A short payment is refused rather than accepted quietly. The
        // installment plan is fixed system-wide, so "they paid a bit less
        // today" has no representation in the data model — it would just
        // silently understate what the student owes.
        if (amount.compareTo(expected) < 0) {
            throw new BusinessRuleException(
                    "Installment 1 is %s. Collecting less isn't supported — the 2-installment plan is fixed."
                            .formatted(expected.toPlainString()));
        }
        if (amount.compareTo(fees.totalFees()) > 0) {
            throw new BusinessRuleException("That's more than the full course fee of %s."
                    .formatted(fees.totalFees().toPlainString()));
        }

        Payment payment = new Payment();
        payment.setStudent(student);
        payment.setEnrollment(enrollment);
        payment.setAmount(amount);
        payment.setInstallmentNumber(1);
        payment.setTotalInstallments(FeeCalculator.TOTAL_INSTALLMENTS);
        payment.setPaymentDate(today);
        payment.setPaymentMode(parseMode(request.paymentMode()));
        payment.setPaymentStatus(Payment.Status.Success);
        payment.setTransactionId(request.transactionId());
        payment.setRemarks(request.remarks());
        payment.setReceiptNo(feeCalculator.receiptNumberFor(student.getStudentId(), 1, today));
        return paymentRepository.save(payment);
    }

    private boolean emailReceipt(Student student, Enrollment enrollment,
                                 Payment payment, FeeBreakdownDto fees) {
        try {
            byte[] pdf = receiptPdfService.render(student, enrollment, payment, fees);

            // Derived from what was actually collected, not from the plan.
            // A student who paid the full fee at the counter must not be
            // emailed that an installment is still due — the attached PDF
            // says "Settled", and the two contradicting each other is worse
            // than either being wrong on its own.
            BigDecimal balance = fees.totalFees().subtract(payment.getAmount());
            String closing = balance.signum() > 0
                    ? "Your balance of %s is due on %s.".formatted(
                            balance.toPlainString(), fees.installment2DueDate())
                    : "Your fees are paid in full — nothing further is due.";

            return emailService.sendWithAttachment(
                    student.getEmail(),
                    "Your SMVITA registration receipt — " + payment.getReceiptNo(),
                    """
                    Dear %s,

                    Welcome to Shriram Mantri Vidyanidhi Info Tech Academy.

                    Your registration for %s is confirmed and you have been enrolled into
                    batch %s. Your receipt is attached.

                    %s

                    Warm regards,
                    SMVITA — ComputerSeekho
                    """.formatted(student.getFirstName(), fees.courseName(),
                            enrollment.getBatch().getBatchName(), closing),
                    payment.getReceiptNo().replace('/', '-') + ".pdf",
                    pdf);
        } catch (Exception ex) {
            // Never fail a completed registration because the receipt email
            // didn't go out — the PDF is still downloadable from the
            // confirmation screen and from the Students list.
            log.warn("Receipt email for student #{} failed: {}", student.getStudentId(), ex.getMessage());
            return false;
        }
    }

    private Student buildStudent(StudentDetailsRequest d, Inquiry inquiry, LocalDate today) {
        Student s = new Student();
        s.setInquiry(inquiry);
        s.setFirstName(d.firstName().trim());
        s.setLastName(d.lastName().trim());
        s.setParentName(d.parentName().trim());
        s.setParentPhone(blankToNull(d.parentPhone()));
        s.setEmail(d.email().trim());
        s.setPhone(d.phone().trim());
        s.setDob(d.dob());
        s.setGender(parseGender(d.gender()));
        s.setAddressLine1(blankToNull(d.addressLine1()));
        s.setAddressLine2(blankToNull(d.addressLine2()));
        s.setCity(blankToNull(d.city()));
        s.setState(blankToNull(d.state()));
        s.setPincode(blankToNull(d.pincode()));
        s.setPhotoUrl(blankToNull(d.photoUrl()));
        s.setQualification(blankToNull(d.qualification()));
        s.setRegDate(today);
        return s;
    }

    private Student.Gender parseGender(String raw) {
        if (raw == null || raw.isBlank()) return null;
        for (Student.Gender g : Student.Gender.values()) {
            if (g.name().equalsIgnoreCase(raw)) return g;
        }
        throw new BusinessRuleException("Unknown gender: '" + raw + "'");
    }

    private Payment.Mode parseMode(String raw) {
        if (raw == null || raw.isBlank()) return Payment.Mode.Cash;
        for (Payment.Mode m : Payment.Mode.values()) {
            if (m.name().equalsIgnoreCase(raw) || m.toDbValue().equalsIgnoreCase(raw)) return m;
        }
        throw new BusinessRuleException("Unknown payment mode: '" + raw + "'");
    }

    private static String blankToNull(String v) {
        return v == null || v.isBlank() ? null : v.trim();
    }

    private static boolean contains(String value, String needle) {
        return value != null && value.toLowerCase().contains(needle);
    }

    private Inquiry getInquiryOrThrow(Integer id) {
        return inquiryRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Enquiry", id));
    }

    private Course getCourseOrThrow(Integer id) {
        return courseRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Course", id));
    }

    /**
     * The course being registered for: the request's if given, the enquiry's
     * otherwise.
     *
     * A course the counsellor deliberately switched to must be active — an
     * inactive course is one the academy has stopped selling, and starting a
     * new student on it is almost certainly a misclick. The enquiry's own
     * course is exempt from that check: it was valid when the lead came in,
     * and refusing to register an existing prospect because the course was
     * retired in the meantime would strand them with no path forward.
     */
    private Course resolveCourse(Integer requestedCourseId, Inquiry inquiry) {
        if (requestedCourseId == null
                || requestedCourseId.equals(inquiry.getCourse().getCourseId())) {
            return inquiry.getCourse();
        }
        Course chosen = getCourseOrThrow(requestedCourseId);
        if (Boolean.FALSE.equals(chosen.getIsActive())) {
            throw new BusinessRuleException(
                    chosen.getName() + " is no longer offered and can't take new registrations.");
        }
        return chosen;
    }

    private InquiryDto toInquirySummary(Inquiry i) {
        return new InquiryDto(
                i.getInquiryId(),
                i.getCourse().getCourseId(), i.getCourse().getName(),
                i.getStaff() != null ? i.getStaff().getStaffId() : null,
                i.getStaff() != null ? i.getStaff().getName() : null,
                i.getEnquirerName(), i.getEmail(), i.getPhone(), i.getMessage(), i.getSource(),
                i.getStatus() != null ? i.getStatus().toDbValue() : null,
                i.getInquiryDate(), null, null, null);
    }

    private BatchDto toBatchSummary(Batch b) {
        long occupied = enrollmentRepository.countByBatch_BatchIdAndStatusNot(
                b.getBatchId(), Enrollment.Status.Dropped);
        return new BatchDto(b.getBatchId(), b.getBatchName(),
                b.getCourse().getCourseId(), b.getCourse().getName(),
                b.getCourse().getCategory().getCategoryId(), b.getCourse().getCategory().getName(),
                b.getAcademicYear(), b.getCapacity(), (int) occupied,
                b.getStatus().name(), b.getIsActive(), 0L);
    }

    private StudentDto toStudentDto(Student s, Enrollment e) {
        return new StudentDto(
                s.getStudentId(), s.getInquiry().getInquiryId(),
                s.getFirstName(), s.getLastName(), s.getParentName(), s.getParentPhone(),
                s.getEmail(), s.getPhone(), s.getDob(),
                s.getGender() != null ? s.getGender().name() : null,
                s.getAddressLine1(), s.getAddressLine2(), s.getCity(), s.getState(), s.getPincode(),
                s.getPhotoUrl(), s.getQualification(), s.getRegDate(),
                e != null ? e.getEnrollmentId() : null,
                e != null ? e.getBatch().getBatchId() : null,
                e != null ? e.getBatch().getBatchName() : null,
                e != null ? e.getBatch().getCourse().getName() : null);
    }

    private PaymentDto toPaymentDto(Payment p) {
        return new PaymentDto(p.getPaymentId(), p.getStudent().getStudentId(),
                p.getStudent().getFirstName() + " " + p.getStudent().getLastName(),
                p.getEnrollment().getEnrollmentId(), p.getAmount(),
                p.getInstallmentNumber(), p.getTotalInstallments(), p.getPaymentDate(),
                p.getPaymentMode().toDbValue(), p.getPaymentStatus().name(),
                p.getTransactionId(), p.getReceiptNo(), p.getRemarks());
    }
}
