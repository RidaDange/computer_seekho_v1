package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.CourseCategory;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface CourseCategoryRepository extends JpaRepository<CourseCategory, Integer> {
    List<CourseCategory> findByIsActiveTrue();
}
