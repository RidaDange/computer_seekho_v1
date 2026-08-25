package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.Banner;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.time.LocalDate;
import java.util.List;

public interface BannerRepository extends JpaRepository<Banner, Integer> {

    @Query("""
            SELECT b FROM Banner b
            WHERE b.isActive = true
              AND (b.startDate IS NULL OR b.startDate <= :today)
              AND (b.endDate IS NULL OR b.endDate >= :today)
            ORDER BY b.displayOrder ASC
            """)
    List<Banner> findCurrentlyValid(@Param("today") LocalDate today);
}
