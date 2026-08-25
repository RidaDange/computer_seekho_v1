package com.smvita.computerseekho.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDate;

@Entity
@Table(name = "inquiries")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Inquiry {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "inquiry_id")
    private Integer inquiryId;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "course_id", nullable = false)
    private Course course;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "staff_id")
    private Staff staff; // assigned counselor

    @Column(name = "enquirer_name", nullable = false, length = 150)
    private String enquirerName;

    @Column(name = "email", length = 150)
    private String email;

    @Column(name = "phone", length = 15)
    private String phone;

    @Column(name = "message", columnDefinition = "TEXT")
    private String message;

    @Column(name = "source", length = 100)
    private String source;

    @Convert(converter = InquiryStatusConverter.class)
    @Column(name = "status", nullable = false,
            columnDefinition = "ENUM('New','In-Followup','Converted','Lost','Not Interested')")
    private Status status = Status.New;

    @Column(name = "inquiry_date", nullable = false)
    private LocalDate inquiryDate = LocalDate.now();

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "closure_reason_id")
    private ClosureReason closureReason;

    // Note: JPA enum constants can't contain hyphens or spaces, so
    // 'In-Followup' / 'Not Interested' are mapped explicitly below rather
    // than relying on Enum.valueOf() matching the DB string automatically.
    public enum Status {
        New, InFollowup, Converted, Lost, NotInterested;

        public String toDbValue() {
            return switch (this) {
                case InFollowup -> "In-Followup";
                case NotInterested -> "Not Interested";
                default -> this.name();
            };
        }

        public static Status fromDbValue(String value) {
            return switch (value) {
                case "In-Followup" -> InFollowup;
                case "Not Interested" -> NotInterested;
                default -> Status.valueOf(value);
            };
        }
    }
}
