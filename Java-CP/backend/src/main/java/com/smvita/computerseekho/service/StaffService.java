package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.StaffCreationResult;
import com.smvita.computerseekho.dto.StaffDto;
import com.smvita.computerseekho.entity.Staff;
import com.smvita.computerseekho.exception.BusinessRuleException;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.StaffRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.security.SecureRandom;
import java.util.List;

@Service
@RequiredArgsConstructor
@Transactional
public class StaffService {

    private final StaffRepository staffRepository;
    private final PasswordEncoder passwordEncoder;

    private static final String TEMP_PASSWORD_CHARS =
            "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
    private static final SecureRandom RANDOM = new SecureRandom();

    public List<StaffDto> findAll() {
        return staffRepository.findAll().stream().map(this::toDto).toList();
    }

    public List<StaffDto> findActiveByRole(Staff.Role role) {
        return staffRepository.findByRoleAndIsActiveTrue(role).stream().map(this::toDto).toList();
    }

    public StaffDto findById(Integer id) {
        return toDto(getEntityOrThrow(id));
    }

    /**
     * Generates and hashes a temporary password server-side — the caller
     * never supplies or sees a password field going in. The plaintext temp
     * password is returned exactly once, in StaffCreationResult, for the
     * admin to share with the new staff member.
     */
    public StaffCreationResult create(StaffDto dto) {
        Staff staff = new Staff();
        applyDto(staff, dto);

        String temporaryPassword = generateTemporaryPassword();
        staff.setPasswordHash(passwordEncoder.encode(temporaryPassword));

        Staff saved = staffRepository.save(staff);
        return new StaffCreationResult(toDto(saved), temporaryPassword);
    }

    // Deliberately does not touch passwordHash — credential changes go
    // through a separate reset flow (Day 3+), not this update path.
    public StaffDto update(Integer id, StaffDto dto) {
        Staff staff = getEntityOrThrow(id);
        applyDto(staff, dto);
        return toDto(staffRepository.save(staff));
    }

    public void delete(Integer id) {
        staffRepository.delete(getEntityOrThrow(id));
    }

    private String generateTemporaryPassword() {
        StringBuilder sb = new StringBuilder(10);
        for (int i = 0; i < 10; i++) {
            sb.append(TEMP_PASSWORD_CHARS.charAt(RANDOM.nextInt(TEMP_PASSWORD_CHARS.length())));
        }
        return sb.toString();
    }

    private Staff getEntityOrThrow(Integer id) {
        return staffRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Staff", id));
    }

    private void applyDto(Staff staff, StaffDto dto) {
        staff.setName(dto.name());
        staff.setEmail(dto.email());
        staff.setPhone(dto.phone());
        try {
            staff.setRole(Staff.Role.valueOf(dto.role()));
        } catch (IllegalArgumentException e) {
            throw new BusinessRuleException("Invalid role: " + dto.role());
        }
        staff.setQualification(dto.qualification());
        staff.setExperience(dto.experience());
        staff.setPhotoUrl(dto.photoUrl());
        staff.setUsername(dto.username());
        staff.setIsActive(dto.isActive() != null ? dto.isActive() : true);
    }

    // Null-tolerant on role: a legacy/hand-edited staff row with a NULL role
    // used to NPE here and surface as a blank Faculty page with a 500 behind
    // it, rather than simply showing the record.
    private StaffDto toDto(Staff s) {
        return new StaffDto(s.getStaffId(), s.getName(), s.getEmail(), s.getPhone(),
                s.getRole() != null ? s.getRole().name() : null,
                s.getQualification(), s.getExperience(), s.getPhotoUrl(),
                s.getUsername(), s.getIsActive());
    }
}
