package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.Inquiry;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Collection;
import java.util.List;

public interface InquiryRepository extends JpaRepository<Inquiry, Integer> {

    /** Active enquiry list — closed enquiries are excluded by the caller's status set. */
    List<Inquiry> findByStatusInOrderByInquiryDateDescInquiryIdDesc(Collection<Inquiry.Status> statuses);

    /**
     * The same list, narrowed to one counselor. A counselor working the CRM
     * wants their own leads, not the whole institute's — filtering in SQL
     * rather than in the service keeps that true no matter how many
     * enquiries accumulate.
     */
    List<Inquiry> findByStaff_StaffIdAndStatusInOrderByInquiryDateDescInquiryIdDesc(
            Integer staffId, Collection<Inquiry.Status> statuses);

    /** Everything for one counselor, including closed and converted — their history. */
    List<Inquiry> findByStaff_StaffIdOrderByInquiryDateDescInquiryIdDesc(Integer staffId);

    /**
     * The "load" half of least-loaded round-robin counselor assignment:
     * how many still-open enquiries this counselor is carrying.
     */
    long countByStaff_StaffIdAndStatusIn(Integer staffId, Collection<Inquiry.Status> statuses);

    /**
     * The "round-robin" half: the most recent enquiry given to this
     * counselor. Used only to break ties between equally-loaded counselors,
     * so the same person doesn't win every tie.
     */
    Inquiry findTopByStaff_StaffIdOrderByInquiryIdDesc(Integer staffId);
}
