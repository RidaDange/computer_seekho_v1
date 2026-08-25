package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.Payment;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface PaymentRepository extends JpaRepository<Payment, Integer> {

    List<Payment> findByStudent_StudentIdOrderByInstallmentNumberAsc(Integer studentId);

    List<Payment> findByEnrollment_EnrollmentIdOrderByInstallmentNumberAsc(Integer enrollmentId);

    boolean existsByEnrollment_EnrollmentIdAndInstallmentNumber(Integer enrollmentId, Integer installmentNumber);

    Optional<Payment> findByReceiptNo(String receiptNo);
}
