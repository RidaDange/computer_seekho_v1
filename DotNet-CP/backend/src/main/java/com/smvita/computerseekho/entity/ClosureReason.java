package com.smvita.computerseekho.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Entity
@Table(name = "closure_reasons")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class ClosureReason {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "reason_id")
    private Integer reasonId;

    @Column(name = "reason_text", nullable = false, unique = true, length = 200)
    private String reasonText;

    @Column(name = "is_active", nullable = false)
    private Boolean isActive = true;
}
