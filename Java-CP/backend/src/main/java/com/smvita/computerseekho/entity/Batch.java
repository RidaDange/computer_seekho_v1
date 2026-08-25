package com.smvita.computerseekho.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDate;
import java.time.LocalDateTime;

@Entity
@Table(name = "batches")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Batch {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "batch_id")
    private Integer batchId;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "course_id", nullable = false)
    private Course course;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "staff_id", nullable = false)
    private Staff staff; // actual teaching faculty for this batch

    @Column(name = "batch_name", nullable = false, length = 100)
    private String batchName;

    @Column(name = "academic_year", length = 20)
    private String academicYear;

    @Column(name = "start_date")
    private LocalDate startDate;

    @Column(name = "end_date")
    private LocalDate endDate;

    @Column(name = "presentation_date")
    private LocalDateTime presentationDate;

    @Column(name = "timing", length = 100)
    private String timing;

    @Column(name = "capacity", nullable = false)
    private Integer capacity = 20;

    // System-calculated from enrollments — never hand-edited (see schema.sql comment).
    @Column(name = "current_count", nullable = false)
    private Integer currentCount = 0;

    @Enumerated(EnumType.STRING)
    @Column(name = "status", nullable = false,
            columnDefinition = "ENUM('Upcoming','Ongoing','Completed','Cancelled')")
    private Status status = Status.Upcoming;

    @Column(name = "is_active", nullable = false)
    private Boolean isActive = true;

    public enum Status { Upcoming, Ongoing, Completed, Cancelled }
}
