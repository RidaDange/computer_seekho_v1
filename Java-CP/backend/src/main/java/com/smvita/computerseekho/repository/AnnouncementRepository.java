package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.Announcement;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.time.LocalDate;
import java.util.List;

public interface AnnouncementRepository extends JpaRepository<Announcement, Integer> {

    // JPA custom query method (backend requirement). Powers the Home page
    // ticker: BRD requires "only valid items will be displayed", i.e.
    // active AND within its start/end date window (either bound optional).
    @Query("""
            SELECT a FROM Announcement a
            WHERE a.isActive = true
              AND (a.startDate IS NULL OR a.startDate <= :today)
              AND (a.endDate IS NULL OR a.endDate >= :today)
            ORDER BY a.displayOrder ASC
            """)
    List<Announcement> findCurrentlyValid(@Param("today") LocalDate today);
}
