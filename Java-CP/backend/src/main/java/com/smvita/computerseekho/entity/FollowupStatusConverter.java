package com.smvita.computerseekho.entity;

import jakarta.persistence.AttributeConverter;
import jakarta.persistence.Converter;

/**
 * followups.status is ENUM('Pending','Done','No Response'). 'No Response'
 * contains a space, so a plain @Enumerated(STRING) would try to write
 * "NoResponse" — a value the column rejects. Mirrors
 * InquiryStatusConverter; see that class for the full reasoning.
 */
@Converter(autoApply = false)
public class FollowupStatusConverter implements AttributeConverter<Followup.Status, String> {

    @Override
    public String convertToDatabaseColumn(Followup.Status status) {
        return status == null ? null : status.toDbValue();
    }

    @Override
    public Followup.Status convertToEntityAttribute(String dbValue) {
        return dbValue == null ? null : Followup.Status.fromDbValue(dbValue);
    }
}
