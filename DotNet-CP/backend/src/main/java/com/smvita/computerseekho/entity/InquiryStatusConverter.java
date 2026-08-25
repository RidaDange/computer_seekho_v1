package com.smvita.computerseekho.entity;

import jakarta.persistence.AttributeConverter;
import jakarta.persistence.Converter;

/**
 * Java enum identifiers can't contain hyphens or spaces, but the DB's
 * inquiries.status ENUM has 'In-Followup' and 'Not Interested' — so a
 * plain @Enumerated(STRING) would write "InFollowup" and "NotInterested"
 * to the database, which don't match any value the ENUM column accepts
 * and would fail at write time. This converter is the explicit bridge
 * between the two representations; Inquiry.status uses @Convert with
 * this class instead of @Enumerated.
 */
@Converter(autoApply = false)
public class InquiryStatusConverter implements AttributeConverter<Inquiry.Status, String> {

    @Override
    public String convertToDatabaseColumn(Inquiry.Status status) {
        return status == null ? null : status.toDbValue();
    }

    @Override
    public Inquiry.Status convertToEntityAttribute(String dbValue) {
        return dbValue == null ? null : Inquiry.Status.fromDbValue(dbValue);
    }
}
