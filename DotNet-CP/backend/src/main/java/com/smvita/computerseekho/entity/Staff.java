package com.smvita.computerseekho.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.math.BigDecimal;

@Entity
@Table(name = "staff")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Staff {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "staff_id")
    private Integer staffId;

    @Column(name = "name", nullable = false, length = 150)
    private String name;

    @Column(name = "email", nullable = false, unique = true, length = 150)
    private String email;

    @Column(name = "phone", length = 15)
    private String phone;

    // Same reasoning as Course.level — native MySQL ENUM needs an explicit
    // columnDefinition or Hibernate's validator mismatches it against VARCHAR.
    @Enumerated(EnumType.STRING)
    @Column(name = "role", nullable = false, columnDefinition = "ENUM('Admin','Counselor','Faculty','Manager','Receptionist')")
    private Role role = Role.Counselor;

    @Column(name = "qualification", length = 200)
    private String qualification;

    @Column(name = "experience", precision = 4, scale = 1)
    private BigDecimal experience;

    @Column(name = "photo_url", length = 500)
    private String photoUrl;

    @Column(name = "username", nullable = false, unique = true, length = 50)
    private String username;

    // Never exposed via DTO in plaintext or hash form — see StaffDto /
    // Table Maintenance note: credential fields go through a dedicated
    // set/reset flow, not a plain grid edit (Knowledge Base, Turn 3).
    @Column(name = "password_hash", nullable = false, length = 255)
    private String passwordHash;

    @Column(name = "is_active", nullable = false)
    private Boolean isActive = true;

    public enum Role { Admin, Counselor, Faculty, Manager, Receptionist }
}
