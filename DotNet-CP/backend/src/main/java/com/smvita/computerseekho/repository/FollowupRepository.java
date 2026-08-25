package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.Followup;
import com.smvita.computerseekho.entity.Inquiry;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.time.LocalDate;
import java.util.Collection;
import java.util.List;

public interface FollowupRepository extends JpaRepository<Followup, Integer> {

    /**
     * The Follow-up page's working list, per BRD: everything due today plus
     * anything still pending from earlier ("today + prior pending"), with
     * closed enquiries hidden — hence the inquiry-status filter rather than
     * a plain date query.
     *
     * The status values come in as bound parameters rather than HQL enum
     * literals: both Followup.status and Inquiry.status go through an
     * AttributeConverter (the DB stores 'No Response' / 'In-Followup'), and
     * converters are applied to parameters but not to inline literals.
     *
     * Oldest first — the enquiry that has been waiting longest is the one
     * closest to the 3-4 day "close it" rule.
     */
    @Query("""
            select f from Followup f
            join fetch f.inquiry i
            join fetch i.course
            left join fetch i.staff
            where f.status = :pendingStatus
              and f.followupDate <= :onOrBefore
              and i.status in :openStatuses
            order by f.followupDate asc, f.followupId asc
            """)
    List<Followup> findDue(@Param("pendingStatus") Followup.Status pendingStatus,
                           @Param("onOrBefore") LocalDate onOrBefore,
                           @Param("openStatuses") Collection<Inquiry.Status> openStatuses);

    /**
     * The mirror image of findDue(): follow-ups already booked but not
     * actionable yet. findDue() deliberately hides these so today's calls
     * aren't buried — but with nowhere to see them, a counselor who
     * schedules a follow-up gets no confirmation it exists. This backs the
     * "Scheduled ahead" section that closes that loop.
     */
    @Query("""
            select f from Followup f
            join fetch f.inquiry i
            join fetch i.course
            left join fetch i.staff
            where f.status = :pendingStatus
              and f.followupDate > :after
              and i.status in :openStatuses
            order by f.followupDate asc, f.followupId asc
            """)
    List<Followup> findUpcoming(@Param("pendingStatus") Followup.Status pendingStatus,
                                @Param("after") LocalDate after,
                                @Param("openStatuses") Collection<Inquiry.Status> openStatuses);

    /**
     * findDue()/findUpcoming() narrowed to one counselor's own workload.
     *
     * Written as separate queries rather than one with a nullable :staffId
     * parameter: Hibernate can't infer the type of a bound null in an
     * "is null or ..." predicate, and the workaround costs more clarity
     * than the duplicated where-clause does.
     */
    @Query("""
            select f from Followup f
            join fetch f.inquiry i
            join fetch i.course
            left join fetch i.staff
            where f.status = :pendingStatus
              and f.followupDate <= :onOrBefore
              and i.status in :openStatuses
              and f.staff.staffId = :staffId
            order by f.followupDate asc, f.followupId asc
            """)
    List<Followup> findDueForStaff(@Param("pendingStatus") Followup.Status pendingStatus,
                                   @Param("onOrBefore") LocalDate onOrBefore,
                                   @Param("openStatuses") Collection<Inquiry.Status> openStatuses,
                                   @Param("staffId") Integer staffId);

    @Query("""
            select f from Followup f
            join fetch f.inquiry i
            join fetch i.course
            left join fetch i.staff
            where f.status = :pendingStatus
              and f.followupDate > :after
              and i.status in :openStatuses
              and f.staff.staffId = :staffId
            order by f.followupDate asc, f.followupId asc
            """)
    List<Followup> findUpcomingForStaff(@Param("pendingStatus") Followup.Status pendingStatus,
                                        @Param("after") LocalDate after,
                                        @Param("openStatuses") Collection<Inquiry.Status> openStatuses,
                                        @Param("staffId") Integer staffId);

    List<Followup> findByInquiry_InquiryIdOrderByFollowupDateDescFollowupIdDesc(Integer inquiryId);

    List<Followup> findByInquiry_InquiryIdAndStatus(Integer inquiryId, Followup.Status status);
}
