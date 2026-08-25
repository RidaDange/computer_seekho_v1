package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.Batch;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface BatchRepository extends JpaRepository<Batch, Integer> {
    // Batchwise Placement page — only completed batches have meaningful
    // placement stats to show.
    List<Batch> findByStatusAndIsActiveTrue(Batch.Status status);

    /**
     * Batches a student can actually be registered into: same course as the
     * enquiry, still running or about to, and active. Filtering by course
     * here rather than in the UI stops someone being enrolled into a batch
     * for a course they never enquired about.
     */
    List<Batch> findByCourse_CourseIdAndIsActiveTrueAndStatusInOrderByStartDateAsc(
            Integer courseId, java.util.Collection<Batch.Status> statuses);

    List<Batch> findAllByOrderByStartDateDescBatchIdDesc();
}
