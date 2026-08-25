package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.BatchAlbum;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;

import java.util.List;
import java.util.Optional;

public interface BatchAlbumRepository extends JpaRepository<BatchAlbum, Integer> {

    // UNIQUE(batch_id) in the schema — one album per batch, so Optional is safe here.
    Optional<BatchAlbum> findByBatch_BatchId(Integer batchId);

    /**
     * Public listing — newest batches first, so the current cohort leads.
     *
     * Written as JPQL rather than a derived method name: ordering by a
     * nested property (batch.startDate) inside a method name is fragile and
     * hard to read. The join fetch also stops the summary mapper firing a
     * query per album just to read the batch and course names.
     */
    @Query("""
            select a from BatchAlbum a
            join fetch a.batch b
            join fetch b.course
            where a.isActive = true
            order by b.startDate desc, a.albumId desc
            """)
    List<BatchAlbum> findPublished();
}
