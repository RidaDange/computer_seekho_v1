package com.smvita.computerseekho.entity;

import jakarta.persistence.AttributeConverter;
import jakarta.persistence.Converter;

/**
 * payments.payment_mode is ENUM('Cash','UPI','Card','Bank Transfer','Cheque').
 * 'Bank Transfer' contains a space, which isn't a legal Java identifier, so
 * a plain @Enumerated(STRING) would write "BankTransfer" — a value the
 * column rejects. Same pattern as InquiryStatusConverter and
 * FollowupStatusConverter; see InquiryStatusConverter for the full reasoning.
 */
@Converter(autoApply = false)
public class PaymentModeConverter implements AttributeConverter<Payment.Mode, String> {

    @Override
    public String convertToDatabaseColumn(Payment.Mode mode) {
        return mode == null ? null : mode.toDbValue();
    }

    @Override
    public Payment.Mode convertToEntityAttribute(String dbValue) {
        return dbValue == null ? null : Payment.Mode.fromDbValue(dbValue);
    }
}
