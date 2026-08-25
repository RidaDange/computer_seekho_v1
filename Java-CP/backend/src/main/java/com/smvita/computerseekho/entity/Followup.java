package com.smvita.computerseekho.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDate;

@Entity
@Table(name = "followups")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Followup {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "followup_id")
    private Integer followupId;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "inquiry_id", nullable = false)
    private Inquiry inquiry;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "staff_id", nullable = false)
    private Staff staff;

    /** The date this follow-up is due / was attempted. */
    @Column(name = "followup_date", nullable = false)
    private LocalDate followupDate = LocalDate.now();

    @Column(name = "notes", columnDefinition = "TEXT")
    private String notes;

    /** When the counselor intends to try again; null once the trail ends. */
    @Column(name = "next_followup")
    private LocalDate nextFollowup;

    // Same reason as Inquiry.status: the DB ENUM contains 'No Response',
    // which isn't a legal Java identifier, so it goes through a converter
    // rather than @Enumerated(STRING).
    @Convert(converter = FollowupStatusConverter.class)
    @Column(name = "status", nullable = false,
            columnDefinition = "ENUM('Pending','Done','No Response')")
    private Status status = Status.Pending;

    public enum Status {
        Pending, Done, NoResponse;

        public String toDbValue() {
            return this == NoResponse ? "No Response" : this.name();
        }

        public static Status fromDbValue(String value) {
            return "No Response".equals(value) ? NoResponse : Status.valueOf(value);
        }
    }
}
