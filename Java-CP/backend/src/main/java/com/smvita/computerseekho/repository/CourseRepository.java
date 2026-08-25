package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.Course;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface CourseRepository extends JpaRepository<Course, Integer> {
    List<Course> findByIsActiveTrue();
}
