package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.ClosureReason;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface ClosureReasonRepository extends JpaRepository<ClosureReason, Integer> {

    // Powers the standard-reasons dropdown on the Close Enquiry screen —
    // only active reasons should appear as selectable options.
    List<ClosureReason> findByIsActiveTrue();
}
