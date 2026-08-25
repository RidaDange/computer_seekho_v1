package com.smvita.computerseekho.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.math.BigDecimal;
import java.time.LocalDate;

@Entity
@Table(name = "placement_records")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class PlacementRecord {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "placement_id")
    private Integer placementId;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "student_id", nullable = false)
    private Student student;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "batch_id")
    private Batch batch;

    // Knowledge Base decision (Turn 3): unified via FK, replacing the
    // free-text company_name the sample flow originally used.
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "recruiter_id", nullable = false)
    private Recruiter recruiter;

    @Column(name = "position", length = 200)
    private String position;

    // placement_drives entity/module is a later (Day 4) deliverable — not
    // needed for Day 2's public pages, so this stays a plain nullable FK
    // column for now rather than a full @ManyToOne relationship.
    @Column(name = "drive_id")
    private Integer driveId;

    @Column(name = "package", precision = 10, scale = 2)
    private BigDecimal packageAmount;

    @Column(name = "placement_date")
    private LocalDate placementDate;

    @Column(name = "is_featured", nullable = false)
    private Boolean isFeatured = false;
}
