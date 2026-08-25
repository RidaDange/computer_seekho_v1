package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.FeeBreakdownDto;
import com.smvita.computerseekho.dto.PaymentDto;
import com.smvita.computerseekho.dto.PaymentRequest;
import com.smvita.computerseekho.entity.Enrollment;
import com.smvita.computerseekho.entity.Payment;
import com.smvita.computerseekho.exception.BusinessRuleException;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.EnrollmentRepository;
import com.smvita.computerseekho.repository.PaymentRepository;
import lombok.RequiredArgsConstructor;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.List;

/**
 * Installments after the first, plus receipt retrieval.
 *
 * Installment 1 is created inside RegistrationService because it's part of
 * the same atomic registration; everything afterwards lands here.
 */
@Service
@RequiredArgsConstructor
@Transactional
public class PaymentService {

    private static final Logger log = LoggerFactory.getLogger(PaymentService.class);

    private final PaymentRepository paymentRepository;
    private final EnrollmentRepository enrollmentRepository;
    private final FeeCalculator feeCalculator;
    private final ReceiptPdfService receiptPdfService;

    @Transactional(readOnly = true)
    public List<PaymentDto> findByStudent(Integer studentId) {
        return paymentRepository.findByStudent_StudentIdOrderByInstallmentNumberAsc(studentId)
                .stream().map(this::toDto).toList();
    }

    @Transactional(readOnly = true)
    public List<PaymentDto> findAll() {
        return paymentRepository.findAll().stream().map(this::toDto).toList();
    }

    /** Collect installment 2. */
    public PaymentDto collectNextInstallment(PaymentRequest request) {
        Enrollment enrollment = enrollmentRepository.findById(request.enrollmentId())
                .orElseThrow(() -> new ResourceNotFoundException("Enrolment", request.enrollmentId()));

        List<Payment> existing = paymentRepository
                .findByEnrollment_EnrollmentIdOrderByInstallmentNumberAsc(request.enrollmentId());

        int next = existing.size() + 1;
        if (next > FeeCalculator.TOTAL_INSTALLMENTS) {
            throw new BusinessRuleException(
                    "All %d installments have already been collected for this enrolment."
                            .formatted(FeeCalculator.TOTAL_INSTALLMENTS));
        }

        LocalDate registeredOn = enrollment.getStudent().getRegDate();
        FeeBreakdownDto fees = feeCalculator.breakdownFor(enrollment.getBatch().getCourse(), registeredOn);

        BigDecimal alreadyPaid = existing.stream()
                .map(Payment::getAmount)
                .reduce(BigDecimal.ZERO, BigDecimal::add);
        BigDecimal outstanding = fees.totalFees().subtract(alreadyPaid);

        if (request.amount().compareTo(outstanding) > 0) {
            throw new BusinessRuleException("Only %s is outstanding on this enrolment."
                    .formatted(outstanding.toPlainString()));
        }

        Payment payment = new Payment();
        payment.setStudent(enrollment.getStudent());
        payment.setEnrollment(enrollment);
        payment.setAmount(request.amount());
        payment.setInstallmentNumber(next);
        payment.setTotalInstallments(FeeCalculator.TOTAL_INSTALLMENTS);
        payment.setPaymentDate(LocalDate.now());
        payment.setPaymentMode(parseMode(request.paymentMode()));
        payment.setPaymentStatus(Payment.Status.Success);
        payment.setTransactionId(request.transactionId());
        payment.setRemarks(request.remarks());
        payment.setReceiptNo(feeCalculator.receiptNumberFor(
                enrollment.getStudent().getStudentId(), next, LocalDate.now()));

        Payment saved = paymentRepository.save(payment);
        log.info("Installment {} of {} collected for student #{} — receipt {}",
                next, FeeCalculator.TOTAL_INSTALLMENTS,
                enrollment.getStudent().getStudentId(), saved.getReceiptNo());
        return toDto(saved);
    }

    /** Re-renders the PDF on demand rather than storing the bytes — the DB stays the source of truth. */
    @Transactional(readOnly = true)
    public byte[] renderReceipt(Integer paymentId) {
        Payment payment = paymentRepository.findById(paymentId)
                .orElseThrow(() -> new ResourceNotFoundException("Payment", paymentId));
        Enrollment enrollment = payment.getEnrollment();
        FeeBreakdownDto fees = feeCalculator.breakdownFor(
                enrollment.getBatch().getCourse(), payment.getStudent().getRegDate());
        return receiptPdfService.render(payment.getStudent(), enrollment, payment, fees);
    }

    @Transactional(readOnly = true)
    public String receiptFilename(Integer paymentId) {
        return paymentRepository.findById(paymentId)
                .map(p -> p.getReceiptNo().replace('/', '-') + ".pdf")
                .orElse("receipt.pdf");
    }

    /** What's still owed on an enrolment — drives the "Collect" button on the Students list. */
    @Transactional(readOnly = true)
    public FeeBreakdownDto feeStatusFor(Integer enrollmentId) {
        Enrollment enrollment = enrollmentRepository.findById(enrollmentId)
                .orElseThrow(() -> new ResourceNotFoundException("Enrolment", enrollmentId));
        return feeCalculator.breakdownFor(
                enrollment.getBatch().getCourse(), enrollment.getStudent().getRegDate());
    }

    private Payment.Mode parseMode(String raw) {
        if (raw == null || raw.isBlank()) return Payment.Mode.Cash;
        for (Payment.Mode m : Payment.Mode.values()) {
            if (m.name().equalsIgnoreCase(raw) || m.toDbValue().equalsIgnoreCase(raw)) return m;
        }
        throw new BusinessRuleException("Unknown payment mode: '" + raw + "'");
    }

    private PaymentDto toDto(Payment p) {
        return new PaymentDto(p.getPaymentId(), p.getStudent().getStudentId(),
                p.getStudent().getFirstName() + " " + p.getStudent().getLastName(),
                p.getEnrollment().getEnrollmentId(), p.getAmount(),
                p.getInstallmentNumber(), p.getTotalInstallments(), p.getPaymentDate(),
                p.getPaymentMode().toDbValue(), p.getPaymentStatus().name(),
                p.getTransactionId(), p.getReceiptNo(), p.getRemarks());
    }
}
