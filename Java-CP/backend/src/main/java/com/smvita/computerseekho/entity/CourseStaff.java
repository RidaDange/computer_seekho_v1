package com.smvita.computerseekho.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDate;

/**
 * Junction table: which faculty teach/are associated with a course.
 * is_primary = true is the single authoritative source for a course's
 * "default faculty" on the Course Detail page (Knowledge Base decision,
 * Turn 2/3) — the old default_faculty_id concept is retired. Uniqueness
 * (at most one is_primary=true per course) is enforced in CourseService,
 * not at the DB level — see the comment there for why.
 */
@Entity
@Table(name = "course_staff", uniqueConstraints = @UniqueConstraint(columnNames = {"course_id", "staff_id"}))
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class CourseStaff {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "course_staff_id")
    private Integer courseStaffId;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "course_id", nullable = false)
    private Course course;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "staff_id", nullable = false)
    private Staff staff;

    @Column(name = "assigned_date")
    private LocalDate assignedDate = LocalDate.now();

    @Column(name = "is_primary", nullable = false)
    private Boolean isPrimary = false;
}
