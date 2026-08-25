package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.Enrollment;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface EnrollmentRepository extends JpaRepository<Enrollment, Integer> {

    /** The batch roster — who's actually in this batch. */
    List<Enrollment> findByBatch_BatchIdOrderByEnrollDateAscEnrollmentIdAsc(Integer batchId);

    List<Enrollment> findByStudent_StudentIdOrderByEnrollmentIdDesc(Integer studentId);

    /**
     * batches.current_count is documented as system-calculated, so it's
     * recomputed from this rather than incremented blindly — an increment
     * that drifts once stays wrong forever.
     *
     * Counts everything except Dropped rather than only Active: a student
     * who finished the course was still in the batch, so counting Active
     * alone would report every completed batch as empty and turn the public
     * "X/Y placed" stat into "1/0".
     */
    long countByBatch_BatchIdAndStatusNot(Integer batchId, Enrollment.Status excluded);

    long countByBatch_BatchIdAndStatus(Integer batchId, Enrollment.Status status);

    /** Guards against enrolling the same student into the same batch twice. */
    boolean existsByStudent_StudentIdAndBatch_BatchId(Integer studentId, Integer batchId);
}
