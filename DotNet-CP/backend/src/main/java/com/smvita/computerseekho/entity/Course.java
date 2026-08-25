package com.smvita.computerseekho.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.math.BigDecimal;

@Entity
@Table(name = "courses")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Course {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "course_id")
    private Integer courseId;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "category_id", nullable = false)
    private CourseCategory category;

    @Column(name = "name", nullable = false, length = 150)
    private String name;

    @Column(name = "description", columnDefinition = "TEXT")
    private String description;

    @Column(name = "duration", length = 50)
    private String duration;

    @Column(name = "fees", nullable = false, precision = 10, scale = 2)
    private BigDecimal fees = BigDecimal.ZERO;

    // Native MySQL ENUM column — columnDefinition must be spelled out or
    // Hibernate's schema validator expects a plain VARCHAR and fails at
    // startup the same way the testimonials.rating TINYINT mismatch did.
    @Enumerated(EnumType.STRING)
    @Column(name = "level", nullable = false, columnDefinition = "ENUM('Beginner','Intermediate','Advanced')")
    private Level level = Level.Beginner;

    @Column(name = "syllabus_url", length = 500)
    private String syllabusUrl;

    @Column(name = "cover_photo", length = 255)
    private String coverPhoto;

    @Column(name = "is_active", nullable = false)
    private Boolean isActive = true;

    public enum Level { Beginner, Intermediate, Advanced }
}
