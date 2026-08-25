package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.CourseStaff;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface CourseStaffRepository extends JpaRepository<CourseStaff, Integer> {

    // Course Detail page's primary-faculty lookup.
    //
    // Returns a List, not an Optional, deliberately: the "at most one
    // is_primary=1 per course" rule is enforced in CourseService, not by the
    // database, so nothing stops a stray second row from existing (a manual
    // INSERT, a seed re-run, an interrupted setPrimaryFaculty). With an
    // Optional return type Spring Data throws
    // IncorrectResultSizeDataAccessException the moment that happens, which
    // took out *every* course read — list and detail alike — with an
    // unexplained 500. Reading a list and taking the first row degrades
    // gracefully instead.
    List<CourseStaff> findByCourse_CourseIdAndIsPrimaryTrueOrderByCourseStaffIdAsc(Integer courseId);

    /**
     * A specific staff member's link to a specific course, primary or not.
     * uk_course_staff makes (course_id, staff_id) unique, so promoting
     * someone who already teaches the course has to update their existing
     * row rather than insert a second one.
     */
    Optional<CourseStaff> findByCourse_CourseIdAndStaff_StaffId(Integer courseId, Integer staffId);

    // Used by CourseService to unset the previous primary before setting a
    // new one — the app-level uniqueness enforcement decided in the
    // Knowledge Base (Turn 3, §9.4) instead of a DB constraint.
    List<CourseStaff> findByCourse_CourseIdAndIsPrimaryTrueAndCourseStaffIdNot(Integer courseId, Integer excludeId);
}
