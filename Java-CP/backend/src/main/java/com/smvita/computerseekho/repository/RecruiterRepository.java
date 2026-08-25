package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.Recruiter;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface RecruiterRepository extends JpaRepository<Recruiter, Integer> {

    // JPA custom query method (backend requirement) — powers the public
    // "Our Recruiters" page, which must show active companies only.
    List<Recruiter> findByIsActiveTrue();

    boolean existsByCompanyNameIgnoreCase(String companyName);
}
