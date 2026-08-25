package com.smvita.computerseekho.dto;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotNull;

import java.math.BigDecimal;

/**
 * The whole registration wizard in one payload, saved as one transaction.
 *
 * Deliberately not three separate endpoints: students, enrollments and
 * payments are only meaningful together. A student row with no enrolment,
 * or an enrolment with no first payment, is a half-registered record that
 * nothing downstream (receipt, batch count, placement) can use — and
 * there'd be no obvious moment to clean it up.
 */
public record RegistrationRequest(

        @NotNull(message = "An enquiry is required — registration can't happen without one")
        Integer inquiryId,

        /**
         * The course actually being registered for. Normally the enquiry's
         * own course, which is what the wizard pre-selects — but a walk-in
         * often enquires about one course and signs up for another, so the
         * counter needs to be able to change it.
         *
         * Left null it falls back to the enquiry's course, which keeps every
         * existing caller working. When it differs, the enquiry is corrected
         * to match rather than left describing a course the student never
         * took.
         */
        Integer courseId,

        @NotNull(message = "Select a batch")
        Integer batchId,

        @Valid
        @NotNull(message = "Student details are required")
        StudentDetailsRequest student,

        /**
         * What's actually being collected at the counter now. Left null it
         * defaults to installment 1 of the course fee; a smaller amount is
         * rejected rather than silently accepted, since the 2-installment
         * split is fixed system-wide (KB §3.2).
         */
        BigDecimal amountPaid,

        String paymentMode,   // Cash | UPI | Card | Bank Transfer | Cheque
        String transactionId,
        String remarks
) {}
