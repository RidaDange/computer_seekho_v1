package com.smvita.computerseekho.service;

import com.smvita.computerseekho.entity.Inquiry;
import com.smvita.computerseekho.entity.Staff;
import com.smvita.computerseekho.repository.InquiryRepository;
import com.smvita.computerseekho.repository.StaffRepository;
import lombok.RequiredArgsConstructor;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.Comparator;
import java.util.List;
import java.util.Optional;
import java.util.Set;

/**
 * KB §9.6: counselor auto-assignment = least-loaded round robin.
 *
 * "Least-loaded" is measured as the number of enquiries the counselor is
 * still actively working (New or In-Followup) — closed and converted
 * enquiries are finished work and shouldn't count against them. Ties are
 * broken round-robin style, by whoever was handed an enquiry least
 * recently, so an idle team doesn't funnel every new lead to the counselor
 * with the lowest staff_id.
 *
 * Pulled out of InquiryService into its own bean because it's the single
 * highest-value piece of business logic on Day 3 and the Day 5 plan calls
 * for Mockito tests on exactly this — far easier to test in isolation.
 */
@Service
@RequiredArgsConstructor
@Transactional(readOnly = true)
public class CounselorAssignmentService {

    private static final Logger log = LoggerFactory.getLogger(CounselorAssignmentService.class);

    /** An enquiry still on someone's plate. */
    public static final Set<Inquiry.Status> OPEN_STATUSES =
            Set.of(Inquiry.Status.New, Inquiry.Status.InFollowup);

    private final StaffRepository staffRepository;
    private final InquiryRepository inquiryRepository;

    /**
     * Returns the counselor who should take the next enquiry, or empty if
     * there are no active Counselors at all. Callers treat empty as
     * "unassigned" rather than an error: an enquiry must never be lost just
     * because nobody is set up to receive it — it stays visible with no
     * counselor until an admin assigns one.
     */
    public Optional<Staff> nextCounselor() {
        List<Staff> counselors = staffRepository.findByRoleAndIsActiveTrue(Staff.Role.Counselor);
        if (counselors.isEmpty()) {
            log.warn("No active Counselor to auto-assign this enquiry to — leaving it unassigned");
            return Optional.empty();
        }

        // Built as a named local rather than inline: starting a comparator
        // chain with Comparator.comparingLong(this::openLoad) leaves the
        // element type in a receiver position, where javac can't infer it.
        Comparator<Staff> leastLoadedThenLongestWaiting =
                Comparator.<Staff>comparingLong(this::openLoad)
                        .thenComparingInt(this::lastAssignedInquiryId)
                        .thenComparingInt(Staff::getStaffId);

        Optional<Staff> chosen = counselors.stream().min(leastLoadedThenLongestWaiting);

        chosen.ifPresent(s -> log.info("Auto-assigned enquiry to counselor '{}' (open load {})",
                s.getName(), openLoad(s)));
        return chosen;
    }

    private long openLoad(Staff counselor) {
        return inquiryRepository.countByStaff_StaffIdAndStatusIn(counselor.getStaffId(), OPEN_STATUSES);
    }

    /**
     * Highest inquiry_id this counselor has ever been given; 0 if never.
     * Lower value == waited longer for their turn, which is what makes the
     * tie-break a rotation rather than a fixed ordering.
     */
    private int lastAssignedInquiryId(Staff counselor) {
        Inquiry latest = inquiryRepository.findTopByStaff_StaffIdOrderByInquiryIdDesc(counselor.getStaffId());
        return latest == null ? 0 : latest.getInquiryId();
    }
}
