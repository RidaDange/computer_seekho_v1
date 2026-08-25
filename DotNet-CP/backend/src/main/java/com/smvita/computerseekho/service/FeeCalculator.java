package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.FeeBreakdownDto;
import com.smvita.computerseekho.entity.Course;
import org.springframework.stereotype.Component;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.time.LocalDate;

/**
 * The single source of truth for what a student owes and when.
 *
 * Knowledge Base decisions encoded here:
 *   §3.2 / Turn 5 — the plan is fixed at 2 installments system-wide, not
 *                   staff-configurable.
 *   Default assumption — a 50/50 split of courses.fees.
 *   Turn 5 open item — the 2nd installment's due date was never specified;
 *                      settled as registration date + 30 days.
 *
 * Kept as its own component so the figure shown on the registration form,
 * the figure written to `payments`, and the figure printed on the PDF all
 * come from one place and cannot disagree.
 */
@Component
public class FeeCalculator {

    public static final int TOTAL_INSTALLMENTS = 2;

    /** Resolved Turn-5 open item: installment 2 falls due 30 days after registration. */
    public static final int INSTALLMENT_2_OFFSET_DAYS = 30;

    public FeeBreakdownDto breakdownFor(Course course, LocalDate registrationDate) {
        BigDecimal total = course.getFees() != null ? course.getFees() : BigDecimal.ZERO;

        // Halve, rounding the first installment up. On an odd amount the
        // student pays the extra paisa first and the remainder settles
        // exactly — the two installments always sum back to the fee, which
        // a plain divide(2, HALF_UP) on both halves can't guarantee.
        BigDecimal first = total.divide(BigDecimal.valueOf(TOTAL_INSTALLMENTS), 2, RoundingMode.CEILING);
        BigDecimal second = total.subtract(first);

        return new FeeBreakdownDto(
                course.getCourseId(),
                course.getName(),
                total.setScale(2, RoundingMode.HALF_UP),
                TOTAL_INSTALLMENTS,
                first,
                registrationDate,
                second,
                registrationDate.plusDays(INSTALLMENT_2_OFFSET_DAYS)
        );
    }

    /**
     * Receipt numbers are derived rather than sequential.
     *
     * A running counter would need either a separate sequence table or a
     * read-then-increment that two concurrent registrations could both win.
     * (studentId, installmentNumber) is already unique by construction, so
     * deriving from it is collision-proof without extra machinery — and
     * still reads sensibly on a printed receipt.
     */
    public String receiptNumberFor(Integer studentId, int installmentNumber, LocalDate date) {
        return "VITA/%s/%05d-%d".formatted(financialYear(date), studentId, installmentNumber);
    }

    /** Indian financial year: April to March, so 2026-07-29 is "2026-27". */
    public String financialYear(LocalDate date) {
        int startYear = date.getMonthValue() >= 4 ? date.getYear() : date.getYear() - 1;
        return "%d-%02d".formatted(startYear, (startYear + 1) % 100);
    }
}
