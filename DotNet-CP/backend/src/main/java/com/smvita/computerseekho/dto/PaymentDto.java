package com.smvita.computerseekho.dto;

import java.math.BigDecimal;
import java.time.LocalDate;

public record PaymentDto(
        Integer paymentId,
        Integer studentId,
        String studentName,
        Integer enrollmentId,
        BigDecimal amount,
        Integer installmentNumber,
        Integer totalInstallments,
        LocalDate paymentDate,
        String paymentMode,
        String paymentStatus,
        String transactionId,
        String receiptNo,
        String remarks
) {}
