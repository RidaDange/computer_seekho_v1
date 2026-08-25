package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.CourseDto;
import com.smvita.computerseekho.entity.Course;
import com.smvita.computerseekho.entity.CourseCategory;
import com.smvita.computerseekho.entity.CourseStaff;
import com.smvita.computerseekho.entity.Staff;
import com.smvita.computerseekho.exception.BusinessRuleException;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.CourseCategoryRepository;
import com.smvita.computerseekho.repository.CourseRepository;
import com.smvita.computerseekho.repository.CourseStaffRepository;
import com.smvita.computerseekho.repository.StaffRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.Optional;

@Service
@RequiredArgsConstructor
@Transactional
public class CourseService {

    private final CourseRepository courseRepository;
    private final CourseCategoryRepository courseCategoryRepository;
    private final CourseStaffRepository courseStaffRepository;
    private final StaffRepository staffRepository;

    public List<CourseDto> findAll() {
        return courseRepository.findAll().stream().map(this::toDto).toList();
    }

    public List<CourseDto> findActive() {
        return courseRepository.findByIsActiveTrue().stream().map(this::toDto).toList();
    }

    public CourseDto findById(Integer id) {
        return toDto(getEntityOrThrow(id));
    }

    public CourseDto create(CourseDto dto) {
        Course course = new Course();
        applyDto(course, dto);
        return toDto(courseRepository.save(course));
    }

    public CourseDto update(Integer id, CourseDto dto) {
        Course course = getEntityOrThrow(id);
        applyDto(course, dto);
        return toDto(courseRepository.save(course));
    }

    public void delete(Integer id) {
        courseRepository.delete(getEntityOrThrow(id));
    }

    /**
     * Sets a course's primary/default faculty. Knowledge Base decision
     * (Turn 3, §9.4): MySQL can't express "at most one is_primary=true per
     * course_id" as a plain constraint, so it's enforced here — unset the
     * previous primary, then set the new one, in a single transaction
     * (the class-level @Transactional makes both writes atomic).
     */
    public void setPrimaryFaculty(Integer courseId, Integer staffId) {
        Course course = getEntityOrThrow(courseId);
        Staff staff = staffRepository.findById(staffId)
                .orElseThrow(() -> new ResourceNotFoundException("Staff", staffId));

        if (staff.getRole() != Staff.Role.Faculty) {
            throw new BusinessRuleException(
                    staff.getName() + " is a " + staff.getRole() + ", not Faculty, and can't be a course's primary faculty.");
        }

        // Demote whoever currently holds is_primary for this course.
        courseStaffRepository.findByCourse_CourseIdAndIsPrimaryTrueOrderByCourseStaffIdAsc(courseId)
                .forEach(current -> {
                    if (!current.getStaff().getStaffId().equals(staffId)) {
                        current.setIsPrimary(false);
                        courseStaffRepository.save(current);
                    }
                });

        // Reuse this staff member's existing link to the course if there is
        // one. Blindly inserting a new row would violate uk_course_staff
        // (course_id, staff_id) any time the person is already listed as a
        // non-primary teacher on the course — which is exactly what happens
        // when you promote an existing team member.
        CourseStaff link = courseStaffRepository
                .findByCourse_CourseIdAndStaff_StaffId(courseId, staffId)
                .orElseGet(() -> {
                    CourseStaff created = new CourseStaff();
                    created.setCourse(course);
                    created.setStaff(staff);
                    return created;
                });

        link.setIsPrimary(true);
        courseStaffRepository.save(link);
    }

    private Course getEntityOrThrow(Integer id) {
        return courseRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Course", id));
    }

    private void applyDto(Course course, CourseDto dto) {
        CourseCategory category = courseCategoryRepository.findById(dto.categoryId())
                .orElseThrow(() -> new ResourceNotFoundException("Course Category", dto.categoryId()));
        course.setCategory(category);
        course.setName(dto.name());
        course.setDescription(dto.description());
        course.setDuration(dto.duration());
        course.setFees(dto.fees());
        if (dto.level() != null) {
            course.setLevel(Course.Level.valueOf(dto.level()));
        }
        course.setSyllabusUrl(dto.syllabusUrl());
        course.setCoverPhoto(dto.coverPhoto());
        course.setIsActive(dto.isActive() != null ? dto.isActive() : true);
    }

    private CourseDto toDto(Course c) {
        Optional<CourseStaff> primary = courseStaffRepository
                .findByCourse_CourseIdAndIsPrimaryTrueOrderByCourseStaffIdAsc(c.getCourseId())
                .stream().findFirst();

        CourseCategory category = c.getCategory();

        return new CourseDto(
                c.getCourseId(),
                category != null ? category.getCategoryId() : null,
                category != null ? category.getName() : null,
                c.getName(),
                c.getDescription(),
                c.getDuration(),
                c.getFees(),
                c.getLevel() != null ? c.getLevel().name() : null,
                c.getSyllabusUrl(),
                c.getCoverPhoto(),
                c.getIsActive(),
                primary.map(cs -> cs.getStaff().getStaffId()).orElse(null),
                primary.map(cs -> cs.getStaff().getName()).orElse(null)
        );
    }
}
