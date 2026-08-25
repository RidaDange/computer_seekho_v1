package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.FollowupDto;
import com.smvita.computerseekho.dto.FollowupLogRequest;
import com.smvita.computerseekho.entity.Followup;
import com.smvita.computerseekho.entity.Inquiry;
import com.smvita.computerseekho.entity.Staff;
import com.smvita.computerseekho.exception.BusinessRuleException;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.FollowupRepository;
import com.smvita.computerseekho.repository.InquiryRepository;
import com.smvita.computerseekho.repository.StaffRepository;
import lombok.RequiredArgsConstructor;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDate;
import java.time.temporal.ChronoUnit;
import java.util.List;

/**
 * The follow-up half of the CRM core — this is the screen a counselor
 * actually lives in, and per the BRD it's the admin panel's landing page.
 */
@Service
@RequiredArgsConstructor
@Transactional
public class FollowupService {

    private static final Logger log = LoggerFactory.getLogger(FollowupService.class);

    private final FollowupRepository followupRepository;
    private final InquiryRepository inquiryRepository;
    private final StaffRepository staffRepository;
    private final CurrentStaffService currentStaffService;

    /**
     * "Today + prior pending, closed enquiries hidden" (Day 3 plan). Anything
     * dated in the future is intentionally left out — it isn't actionable
     * yet and would bury the calls that actually need making today.
     */
    @Transactional(readOnly = true)
    public List<FollowupDto> findDue() {
        return followupRepository
                .findDue(Followup.Status.Pending, LocalDate.now(), CounselorAssignmentService.OPEN_STATUSES)
                .stream().map(this::toDto).toList();
    }

    /** Due list narrowed to the signed-in counselor's own leads. */
    @Transactional(readOnly = true)
    public List<FollowupDto> findDue(String username, boolean mineOnly) {
        if (!mineOnly) {
            return findDue();
        }
        Integer staffId = currentStaffService.requireStaffId(username);
        return followupRepository
                .findDueForStaff(Followup.Status.Pending, LocalDate.now(),
                        CounselorAssignmentService.OPEN_STATUSES, staffId)
                .stream().map(this::toDto).toList();
    }

    /** Upcoming list narrowed to the signed-in counselor's own leads. */
    @Transactional(readOnly = true)
    public List<FollowupDto> findUpcoming(String username, boolean mineOnly) {
        if (!mineOnly) {
            return findUpcoming();
        }
        Integer staffId = currentStaffService.requireStaffId(username);
        return followupRepository
                .findUpcomingForStaff(Followup.Status.Pending, LocalDate.now(),
                        CounselorAssignmentService.OPEN_STATUSES, staffId)
                .stream().map(this::toDto).toList();
    }

    /**
     * Follow-ups scheduled for a future date — not actionable today, but a
     * counselor needs to be able to see that the one they just booked
     * actually landed.
     */
    @Transactional(readOnly = true)
    public List<FollowupDto> findUpcoming() {
        return followupRepository
                .findUpcoming(Followup.Status.Pending, LocalDate.now(), CounselorAssignmentService.OPEN_STATUSES)
                .stream().map(this::toDto).toList();
    }

    /** Full history for one enquiry, newest first — the audit trail behind a lead. */
    @Transactional(readOnly = true)
    public List<FollowupDto> findByInquiry(Integer inquiryId) {
        return followupRepository
                .findByInquiry_InquiryIdOrderByFollowupDateDescFollowupIdDesc(inquiryId)
                .stream().map(this::toDto).toList();
    }

    /**
     * Records the outcome of a follow-up attempt and, if the counselor
     * supplied a next date, opens the next one in the same transaction — so
     * a lead can never quietly fall off the working list between the two
     * writes.
     *
     * The enquiry moves New -> In-Followup on the first logged attempt,
     * which is what distinguishes "nobody has touched this yet" from
     * "actively being worked" on the enquiry list.
     */
    public FollowupDto logOutcome(Integer followupId, FollowupLogRequest request) {
        Followup followup = followupRepository.findById(followupId)
                .orElseThrow(() -> new ResourceNotFoundException("Follow-up", followupId));

        if (followup.getStatus() != Followup.Status.Pending) {
            throw new BusinessRuleException("Follow-up #" + followupId + " has already been logged.");
        }

        Followup.Status outcome = parseStatus(request.status());
        if (outcome == Followup.Status.Pending) {
            throw new BusinessRuleException("Record an outcome of 'Done' or 'No Response'.");
        }

        Inquiry inquiry = followup.getInquiry();
        if (inquiry.getStatus() == Inquiry.Status.Lost
                || inquiry.getStatus() == Inquiry.Status.NotInterested) {
            throw new BusinessRuleException("Enquiry #" + inquiry.getInquiryId()
                    + " is closed — reopen it before logging further follow-ups.");
        }

        followup.setStatus(outcome);
        followup.setFollowupDate(LocalDate.now());
        if (request.notes() != null && !request.notes().isBlank()) {
            followup.setNotes(request.notes().trim());
        }
        followup.setNextFollowup(request.nextFollowup());
        followupRepository.save(followup);

        if (inquiry.getStatus() == Inquiry.Status.New) {
            inquiry.setStatus(Inquiry.Status.InFollowup);
            inquiryRepository.save(inquiry);
        }

        if (request.nextFollowup() != null) {
            if (request.nextFollowup().isBefore(LocalDate.now())) {
                throw new BusinessRuleException("The next follow-up date can't be in the past.");
            }
            createNext(inquiry, followup.getStaff(), request.nextFollowup());
        }

        log.info("Follow-up #{} logged as {} on enquiry #{}",
                followupId, outcome.toDbValue(), inquiry.getInquiryId());
        return toDto(followup);
    }

    /**
     * Manually schedule a follow-up — used when an enquiry has no open one
     * left (e.g. it arrived with no active counselor to auto-assign to, so
     * no first follow-up was created).
     */
    public FollowupDto schedule(Integer inquiryId, Integer staffId, LocalDate date) {
        Inquiry inquiry = inquiryRepository.findById(inquiryId)
                .orElseThrow(() -> new ResourceNotFoundException("Enquiry", inquiryId));
        Staff staff = staffRepository.findById(staffId)
                .orElseThrow(() -> new ResourceNotFoundException("Staff", staffId));

        if (!followupRepository.findByInquiry_InquiryIdAndStatus(inquiryId, Followup.Status.Pending).isEmpty()) {
            throw new BusinessRuleException("Enquiry #" + inquiryId + " already has an open follow-up.");
        }

        // Keep the enquiry's counselor and the follow-up owner in step —
        // otherwise the enquiry list and the follow-up list disagree about
        // whose lead this is.
        if (inquiry.getStaff() == null) {
            inquiry.setStaff(staff);
            inquiryRepository.save(inquiry);
        }

        return toDto(createNext(inquiry, staff, date));
    }

    private Followup createNext(Inquiry inquiry, Staff staff, LocalDate date) {
        Followup next = new Followup();
        next.setInquiry(inquiry);
        next.setStaff(staff);
        next.setFollowupDate(date);
        next.setStatus(Followup.Status.Pending);
        return followupRepository.save(next);
    }

    private Followup.Status parseStatus(String raw) {
        try {
            return Followup.Status.fromDbValue(raw);
        } catch (IllegalArgumentException ex) {
            throw new BusinessRuleException("Unknown follow-up outcome: '" + raw + "'");
        }
    }

    private FollowupDto toDto(Followup f) {
        Inquiry i = f.getInquiry();
        Staff s = f.getStaff();

        return new FollowupDto(
                f.getFollowupId(),
                i != null ? i.getInquiryId() : null,
                i != null ? i.getEnquirerName() : null,
                i != null ? i.getEmail() : null,
                i != null ? i.getPhone() : null,
                i != null && i.getCourse() != null ? i.getCourse().getName() : null,
                s != null ? s.getStaffId() : null,
                s != null ? s.getName() : null,
                f.getFollowupDate(),
                f.getNotes(),
                f.getNextFollowup(),
                f.getStatus() != null ? f.getStatus().toDbValue() : null,
                i != null && i.getStatus() != null ? i.getStatus().toDbValue() : null,
                f.getFollowupDate() != null
                        ? ChronoUnit.DAYS.between(f.getFollowupDate(), LocalDate.now())
                        : 0
        );
    }
}
