package com.smvita.computerseekho.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.math.BigDecimal;
import java.time.LocalDate;

/**
 * One installment against an enrolment.
 *
 * The schema stays general (installment_number / total_installments) while
 * the business rule fixes total_installments at 2 system-wide — KB §3.2,
 * resolved in Turn 5. Keeping the column general means a future change of
 * policy is a service-layer edit, not a migration.
 */
@Entity
@Table(name = "payments")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Payment {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "payment_id")
    private Integer paymentId;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "student_id", nullable = false)
    private Student student;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "enrollment_id", nullable = false)
    private Enrollment enrollment;

    @Column(name = "amount", nullable = false, precision = 10, scale = 2)
    private BigDecimal amount;

    @Column(name = "installment_number", nullable = false)
    private Integer installmentNumber = 1;

    @Column(name = "total_installments", nullable = false)
    private Integer totalInstallments = 2;

    @Column(name = "payment_date", nullable = false)
    private LocalDate paymentDate = LocalDate.now();

    @Convert(converter = PaymentModeConverter.class)
    @Column(name = "payment_mode", nullable = false,
            columnDefinition = "ENUM('Cash','UPI','Card','Bank Transfer','Cheque')")
    private Mode paymentMode = Mode.Cash;

    @Enumerated(EnumType.STRING)
    @Column(name = "payment_status", nullable = false,
            columnDefinition = "ENUM('Success','Pending','Failed','Refunded')")
    private Status paymentStatus = Status.Success;

    @Column(name = "transaction_id", length = 100)
    private String transactionId;

    /** Unique, human-readable, printed on the PDF. Generated in PaymentService. */
    @Column(name = "receipt_no", nullable = false, unique = true, length = 100)
    private String receiptNo;

    @Column(name = "remarks", columnDefinition = "TEXT")
    private String remarks;

    public enum Mode {
        Cash, UPI, Card, BankTransfer, Cheque;

        public String toDbValue() {
            return this == BankTransfer ? "Bank Transfer" : this.name();
        }

        public static Mode fromDbValue(String value) {
            return "Bank Transfer".equals(value) ? BankTransfer : Mode.valueOf(value);
        }
    }

    public enum Status { Success, Pending, Failed, Refunded }
}
