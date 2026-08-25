package com.smvita.computerseekho.dto;

/**
 * Everything the confirmation screen needs after a successful registration:
 * who was created, where they were enrolled, and the receipt to hand over.
 */
public record RegistrationResult(
        StudentDto student,
        PaymentDto firstPayment,
        FeeBreakdownDto feeBreakdown,
        String receiptDownloadPath,
        boolean receiptEmailed
) {}
