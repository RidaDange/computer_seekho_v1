package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.PlacementRecord;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface PlacementRecordRepository extends JpaRepository<PlacementRecord, Integer> {

    // Powers the Placement Detail page — all placements for one batch.
    List<PlacementRecord> findByBatch_BatchId(Integer batchId);

    // Powers the recruiter drill-down: "click TCS -> see placed students"
    // (Turn 4 requirement — validates the recruiter_id FK unification).
    List<PlacementRecord> findByRecruiter_RecruiterId(Integer recruiterId);

    // Powers the "X/Y placed" stat on the Batchwise Placement page.
    long countByBatch_BatchId(Integer batchId);

    /** Admin placement-entry screen — newest first. */
    List<PlacementRecord> findAllByOrderByPlacementDateDescPlacementIdDesc();

    /** A student is placed once per drive/company — guards double entry. */
    boolean existsByStudent_StudentIdAndRecruiter_RecruiterId(Integer studentId, Integer recruiterId);
}
