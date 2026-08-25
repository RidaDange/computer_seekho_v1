package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.BatchDto;
import com.smvita.computerseekho.dto.FeeBreakdownDto;
import com.smvita.computerseekho.dto.InquiryDto;
import com.smvita.computerseekho.dto.RegistrationRequest;
import com.smvita.computerseekho.dto.RegistrationResult;
import com.smvita.computerseekho.service.RegistrationService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

/**
 * The registration wizard's three steps, plus the commit.
 *
 * Separate from StudentController because registering isn't "creating a
 * student" — it's one transaction across students, enrollments, payments
 * and inquiries, and giving it its own path makes that visible rather than
 * hiding it behind POST /students.
 */
@RestController
@RequestMapping("/registrations")
@RequiredArgsConstructor
public class RegistrationController {

    private final RegistrationService registrationService;

    /** Step 1 — find the enquiry. Only enquiries not already registered come back. */
    @GetMapping("/eligible-inquiries")
    public List<InquiryDto> searchInquiries(@RequestParam(required = false) String q) {
        return registrationService.searchRegisterableInquiries(q);
    }

    /** Step 3 — batches for the enquiry's course that can still take students. */
    @GetMapping("/inquiries/{inquiryId}/batches")
    public List<BatchDto> joinableBatches(@PathVariable Integer inquiryId) {
        return registrationService.joinableBatchesFor(inquiryId);
    }

    /** Step 3 — the auto-populated fee and its 2-installment split. */
    @GetMapping("/inquiries/{inquiryId}/fees")
    public FeeBreakdownDto fees(@PathVariable Integer inquiryId) {
        return registrationService.feeBreakdownFor(inquiryId);
    }

    /**
     * Step 3, after the counsellor changes the course dropdown — batches for
     * the chosen course rather than the enquiry's. Keyed by course and not by
     * enquiry because at this point the enquiry no longer determines the
     * answer.
     */
    @GetMapping("/courses/{courseId}/batches")
    public List<BatchDto> joinableBatchesByCourse(@PathVariable Integer courseId) {
        return registrationService.joinableBatchesForCourse(courseId);
    }

    /** Step 3 — the fee for the chosen course, recomputed when it changes. */
    @GetMapping("/courses/{courseId}/fees")
    public FeeBreakdownDto feesByCourse(@PathVariable Integer courseId) {
        return registrationService.feeBreakdownForCourse(courseId);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public RegistrationResult register(@Valid @RequestBody RegistrationRequest request) {
        return registrationService.register(request);
    }
}
