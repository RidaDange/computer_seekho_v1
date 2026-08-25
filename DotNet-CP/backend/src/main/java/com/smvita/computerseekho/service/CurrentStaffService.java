package com.smvita.computerseekho.service;

import com.smvita.computerseekho.entity.Staff;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.StaffRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

/**
 * Resolves the JWT's subject (a username) to the Staff row behind it.
 *
 * The token deliberately carries only the username, so anything that needs
 * "which staff member is this, really" — the CRM's mine-vs-everyone
 * filtering, ownership checks — comes through here rather than trusting a
 * staffId claim the client could have tampered with.
 */
@Service
@RequiredArgsConstructor
@Transactional(readOnly = true)
public class CurrentStaffService {

    private final StaffRepository staffRepository;

    public Staff require(String username) {
        if (username == null || username.isBlank()) {
            throw new ResourceNotFoundException("No signed-in staff member on this request");
        }
        return staffRepository.findByUsername(username)
                .orElseThrow(() -> new ResourceNotFoundException(
                        "Staff account '" + username + "' no longer exists"));
    }

    public Integer requireStaffId(String username) {
        return require(username).getStaffId();
    }
}
