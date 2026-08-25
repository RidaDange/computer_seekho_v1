package com.smvita.computerseekho.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDateTime;

@Entity
@Table(name = "testimonials")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Testimonial {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "testimonial_id")
    private Integer testimonialId;

    @Column(name = "name", nullable = false, length = 150)
    private String name;

    @Column(name = "content", nullable = false, columnDefinition = "TEXT")
    private String content;

    // TINYINT in the DB (schema.sql) — must be Byte, not Integer, or
    // Hibernate's schema validation fails at startup ("found tinyint,
    // expecting integer"). The DTO still exposes this as a plain Integer
    // for a nicer API contract; conversion happens in TestimonialService.
    @Column(name = "rating")
    private Byte rating;

    @Column(name = "photo_url", length = 500)
    private String photoUrl;

    @Column(name = "is_approved", nullable = false)
    private Boolean isApproved = false;

    @Column(name = "created_at", nullable = false, updatable = false)
    private LocalDateTime createdAt = LocalDateTime.now();
}
