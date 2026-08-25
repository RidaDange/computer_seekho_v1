package com.smvita.computerseekho.dto;

import java.math.BigDecimal;
import java.time.LocalDate;

/**
 * What the registration form shows the moment a course is picked: the fee
 * auto-populates and the installment split is displayed (KB §3.2).
 *
 * Computed server-side rather than in the React form so the number printed
 * on the receipt and the number shown on screen can't disagree.
 */
public record FeeBreakdownDto(
        Integer courseId,
        String courseName,
        BigDecimal totalFees,
        int totalInstallments,
        BigDecimal installment1Amount,
        LocalDate installment1DueDate,
        BigDecimal installment2Amount,
        LocalDate installment2DueDate
) {}
