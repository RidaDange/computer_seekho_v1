package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.Staff;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface StaffRepository extends JpaRepository<Staff, Integer> {
    List<Staff> findByRoleAndIsActiveTrue(Staff.Role role);
    Optional<Staff> findByUsername(String username);
}
