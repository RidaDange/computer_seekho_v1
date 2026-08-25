package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.CloseInquiryRequest;
import com.smvita.computerseekho.dto.InquiryCreateRequest;
import com.smvita.computerseekho.dto.InquiryDto;
import com.smvita.computerseekho.entity.ClosureReason;
import com.smvita.computerseekho.entity.Course;
import com.smvita.computerseekho.entity.Followup;
import com.smvita.computerseekho.entity.Inquiry;
import com.smvita.computerseekho.entity.Staff;
import com.smvita.computerseekho.exception.BusinessRuleException;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.ClosureReasonRepository;
import com.smvita.computerseekho.repository.CourseRepository;
import com.smvita.computerseekho.repository.FollowupRepository;
import com.smvita.computerseekho.repository.InquiryRepository;
import lombok.RequiredArgsConstructor;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDate;
import java.util.List;
import java.util.Optional;
import java.util.Set;

/**
 * The enquiry half of the CRM core.
 *
 * Business rules encoded here, all from the Knowledge Base:
 *   §2      first follow-up defaults to enquiry date + 3 days
 *   §3.1    a confirmation email fires to the enquirer (and only the enquirer)
 *   §9.6    the counselor is auto-assigned, least-loaded round robin
 *   §3.2    closing an enquiry requires a reason from closure_reasons
 *   §3.2    closed enquiries drop out of the active lists
 */
@Service
@RequiredArgsConstructor
@Transactional
public class InquiryService {

    private static final Logger log = LoggerFactory.getLogger(InquiryService.class);

    /** KB §2 — "Follow-up default: 3 days after the enquiry itself". */
    private static final int FIRST_FOLLOWUP_OFFSET_DAYS = 3;

    /** The statuses an enquiry closes into; each one demands a reason. */
    private static final Set<Inquiry.Status> CLOSING_STATUSES =
            Set.of(Inquiry.Status.Lost, Inquiry.Status.NotInterested);

    private final InquiryRepository inquiryRepository;
    private final FollowupRepository followupRepository;
    private final CourseRepository courseRepository;
    private final ClosureReasonRepository closureReasonRepository;
    private final CounselorAssignmentService counselorAssignmentService;
    private final CurrentStaffService currentStaffService;
    private final EmailService emailService;

    // ---------------------------------------------------------------- reads

    @Transactional(readOnly = true)
    public List<InquiryDto> findAll() {
        return inquiryRepository.findAll().stream().map(this::toDto).toList();
    }

    /**
     * Everything, optionally narrowed to the signed-in counselor's own
     * leads. `mineOnly` is resolved server-side from the token rather than
     * taking a staffId from the client — otherwise anyone could read a
     * colleague's pipeline by changing a query parameter.
     */
    @Transactional(readOnly = true)
    public List<InquiryDto> findAll(String username, boolean mineOnly) {
        if (!mineOnly) {
            return findAll();
        }
        Integer staffId = currentStaffService.requireStaffId(username);
        return inquiryRepository.findByStaff_StaffIdOrderByInquiryDateDescInquiryIdDesc(staffId)
                .stream().map(this::toDto).toList();
    }

    /**
     * The active Enquiry list. Closed enquiries (Lost / Not Interested) and
     * converted ones are excluded here rather than filtered in the UI, so
     * every caller gets the KB's "closed enquiries no longer appear"
     * behaviour for free.
     */
    @Transactional(readOnly = true)
    public List<InquiryDto> findActive() {
        return inquiryRepository
                .findByStatusInOrderByInquiryDateDescInquiryIdDesc(CounselorAssignmentService.OPEN_STATUSES)
                .stream().map(this::toDto).toList();
    }

    /** The working list, narrowed to the signed-in counselor. */
    @Transactional(readOnly = true)
    public List<InquiryDto> findActive(String username, boolean mineOnly) {
        if (!mineOnly) {
            return findActive();
        }
        Integer staffId = currentStaffService.requireStaffId(username);
        return inquiryRepository
                .findByStaff_StaffIdAndStatusInOrderByInquiryDateDescInquiryIdDesc(
                        staffId, CounselorAssignmentService.OPEN_STATUSES)
                .stream().map(this::toDto).toList();
    }

    @Transactional(readOnly = true)
    public InquiryDto findById(Integer id) {
        return toDto(getEntityOrThrow(id));
    }

    // --------------------------------------------------------------- writes

    /**
     * Creates an enquiry from either channel — the public website form or a
     * staff member entering a walk-in. Identical rules apply to both; only
     * the recorded `source` differs, which is why both controllers funnel
     * into this one method.
     */
    public InquiryDto create(InquiryCreateRequest request, String defaultSource) {
        Course course = courseRepository.findById(request.courseId())
                .orElseThrow(() -> new ResourceNotFoundException("Course", request.courseId()));

        if (Boolean.FALSE.equals(course.getIsActive())) {
            throw new BusinessRuleException("'" + course.getName() + "' is not currently open for enquiries.");
        }

        Inquiry inquiry = new Inquiry();
        inquiry.setCourse(course);
        inquiry.setEnquirerName(request.enquirerName().trim());
        inquiry.setEmail(request.email().trim());
        inquiry.setPhone(request.phone().trim());
        inquiry.setMessage(request.message());
        inquiry.setSource(hasText(request.source()) ? request.source().trim() : defaultSource);
        inquiry.setStatus(Inquiry.Status.New);
        inquiry.setInquiryDate(LocalDate.now());

        Optional<Staff> counselor = counselorAssignmentService.nextCounselor();
        counselor.ifPresent(inquiry::setStaff);

        Inquiry saved = inquiryRepository.save(inquiry);

        // The first follow-up is scheduled up front rather than waiting for
        // someone to remember: an enquiry with no follow-up row would never
        // surface on the counselor's working list at all.
        counselor.ifPresent(staff -> scheduleFirstFollowup(saved, staff));

        sendConfirmationEmail(saved);

        log.info("Enquiry #{} created from '{}' for course '{}'",
                saved.getInquiryId(), saved.getSource(), course.getName());
        return toDto(saved);
    }

    /**
     * Closes an enquiry with a mandatory reason. Deliberately refuses to
     * close something already closed — a second close would silently
     * overwrite the original reason and lose why it really ended.
     */
    public InquiryDto close(Integer id, CloseInquiryRequest request) {
        Inquiry inquiry = getEntityOrThrow(id);

        Inquiry.Status target = parseStatus(request.status());
        if (!CLOSING_STATUSES.contains(target)) {
            throw new BusinessRuleException(
                    "'" + request.status() + "' is not a closing status. Use 'Lost' or 'Not Interested'.");
        }
        if (inquiry.getStatus() == Inquiry.Status.Converted) {
            throw new BusinessRuleException("Enquiry #" + id + " already converted and can't be closed.");
        }
        if (CLOSING_STATUSES.contains(inquiry.getStatus())) {
            throw new BusinessRuleException("Enquiry #" + id + " is already closed.");
        }

        ClosureReason reason = closureReasonRepository.findById(request.closureReasonId())
                .orElseThrow(() -> new ResourceNotFoundException("Closure Reason", request.closureReasonId()));
        if (Boolean.FALSE.equals(reason.getIsActive())) {
            throw new BusinessRuleException("'" + reason.getReasonText() + "' is no longer an active closure reason.");
        }

        inquiry.setStatus(target);
        inquiry.setClosureReason(reason);

        // Retire any outstanding follow-up so it stops appearing on the
        // working list. The list query already hides follow-ups belonging to
        // closed enquiries, but leaving rows in Pending forever would make
        // any future reporting on "still pending" wrong.
        List<Followup> pending = followupRepository
                .findByInquiry_InquiryIdAndStatus(id, Followup.Status.Pending);
        pending.forEach(f -> {
            f.setStatus(Followup.Status.NoResponse);
            f.setNextFollowup(null);
        });
        followupRepository.saveAll(pending);

        log.info("Enquiry #{} closed as {} — reason: {}", id, target.toDbValue(), reason.getReasonText());
        return toDto(inquiryRepository.save(inquiry));
    }

    /** Marks an enquiry as converted — the Day 4 registration flow's entry point. */
    public InquiryDto markConverted(Integer id) {
        Inquiry inquiry = getEntityOrThrow(id);
        if (CLOSING_STATUSES.contains(inquiry.getStatus())) {
            throw new BusinessRuleException("Enquiry #" + id + " is closed and can't be converted.");
        }
        inquiry.setStatus(Inquiry.Status.Converted);
        inquiry.setClosureReason(null);
        return toDto(inquiryRepository.save(inquiry));
    }

    // -------------------------------------------------------------- helpers

    private void scheduleFirstFollowup(Inquiry inquiry, Staff staff) {
        Followup followup = new Followup();
        followup.setInquiry(inquiry);
        followup.setStaff(staff);
        followup.setFollowupDate(inquiry.getInquiryDate().plusDays(FIRST_FOLLOWUP_OFFSET_DAYS));
        followup.setStatus(Followup.Status.Pending);
        followup.setNotes("Auto-scheduled first follow-up (enquiry date + "
                + FIRST_FOLLOWUP_OFFSET_DAYS + " days).");
        followupRepository.save(followup);
    }

    private void sendConfirmationEmail(Inquiry inquiry) {
        // KB §7/8 item 1: the enquirer only — no counselor-notification copy.
        // sendSafely, not send: a misconfigured SMTP server must not roll
        // back an enquiry that was otherwise captured correctly.
        String body = """
                Dear %s,

                Thank you for your enquiry about %s at Shriram Mantri Vidyanidhi Info Tech Academy.

                We have received your details and one of our counsellors will contact you shortly.
                Your enquiry reference number is #%d.

                Warm regards,
                SMVITA — ComputerSeekho
                """.formatted(inquiry.getEnquirerName(), inquiry.getCourse().getName(), inquiry.getInquiryId());

        emailService.sendSafely(inquiry.getEmail(),
                "We've received your enquiry — SMVITA (Ref #" + inquiry.getInquiryId() + ")", body);
    }

    private Inquiry.Status parseStatus(String raw) {
        try {
            return Inquiry.Status.fromDbValue(raw);
        } catch (IllegalArgumentException ex) {
            throw new BusinessRuleException("Unknown enquiry status: '" + raw + "'");
        }
    }

    private Inquiry getEntityOrThrow(Integer id) {
        return inquiryRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Enquiry", id));
    }

    private static boolean hasText(String value) {
        return value != null && !value.isBlank();
    }

    private InquiryDto toDto(Inquiry i) {
        Course course = i.getCourse();
        Staff staff = i.getStaff();
        ClosureReason reason = i.getClosureReason();

        LocalDate nextFollowup = followupRepository
                .findByInquiry_InquiryIdAndStatus(i.getInquiryId(), Followup.Status.Pending)
                .stream()
                .map(Followup::getFollowupDate)
                .min(LocalDate::compareTo)
                .orElse(null);

        return new InquiryDto(
                i.getInquiryId(),
                course != null ? course.getCourseId() : null,
                course != null ? course.getName() : null,
                staff != null ? staff.getStaffId() : null,
                staff != null ? staff.getName() : null,
                i.getEnquirerName(),
                i.getEmail(),
                i.getPhone(),
                i.getMessage(),
                i.getSource(),
                i.getStatus() != null ? i.getStatus().toDbValue() : null,
                i.getInquiryDate(),
                reason != null ? reason.getReasonId() : null,
                reason != null ? reason.getReasonText() : null,
                nextFollowup
        );
    }
}
