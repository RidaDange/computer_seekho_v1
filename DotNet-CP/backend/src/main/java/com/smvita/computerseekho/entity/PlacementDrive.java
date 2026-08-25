package com.smvita.computerseekho.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.math.BigDecimal;
import java.time.LocalDate;

/**
 * A recruiter's visit. Distinct from a placement_record: a drive is the
 * event (TCS came on the 12th, 40 openings), a record is an outcome (Rahul
 * got one of them). Recruiter identity is the FK, not free text — KB §9.3.
 */
@Entity
@Table(name = "placement_drives")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class PlacementDrive {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "drive_id")
    private Integer driveId;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "recruiter_id", nullable = false)
    private Recruiter recruiter;

    /** Nullable: an open drive isn't tied to a single course. */
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "course_id")
    private Course course;

    @Column(name = "drive_date", nullable = false)
    private LocalDate driveDate;

    @Enumerated(EnumType.STRING)
    @Column(name = "drive_mode", nullable = false,
            columnDefinition = "ENUM('Online','Offline','Hybrid')")
    private Mode driveMode = Mode.Offline;

    @Column(name = "position", nullable = false, length = 100)
    private String position;

    @Column(name = "description", columnDefinition = "TEXT")
    private String description;

    @Column(name = "eligibility_criteria", columnDefinition = "TEXT")
    private String eligibilityCriteria;

    @Column(name = "package", precision = 10, scale = 2)
    private BigDecimal packageAmount;

    @Column(name = "hr_contact_name", length = 150)
    private String hrContactName;

    @Column(name = "hr_contact_email", length = 150)
    private String hrContactEmail;

    @Column(name = "hr_contact_phone", length = 15)
    private String hrContactPhone;

    @Column(name = "no_of_openings")
    private Integer noOfOpenings;

    @Column(name = "no_of_students_selected")
    private Integer noOfStudentsSelected;

    @Enumerated(EnumType.STRING)
    @Column(name = "drive_status", nullable = false,
            columnDefinition = "ENUM('Scheduled','Completed','Cancelled')")
    private Status driveStatus = Status.Scheduled;

    public enum Mode { Online, Offline, Hybrid }

    public enum Status { Scheduled, Completed, Cancelled }
}
