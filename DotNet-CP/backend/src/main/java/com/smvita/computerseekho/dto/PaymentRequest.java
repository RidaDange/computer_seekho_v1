package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Positive;

import java.math.BigDecimal;

/** Collecting installment 2 (or re-recording a failed attempt). */
public record PaymentRequest(

        @NotNull(message = "Enrolment is required")
        Integer enrollmentId,

        @NotNull(message = "Amount is required")
        @Positive(message = "Amount must be greater than zero")
        BigDecimal amount,

        String paymentMode,
        String transactionId,
        String remarks
) {}
