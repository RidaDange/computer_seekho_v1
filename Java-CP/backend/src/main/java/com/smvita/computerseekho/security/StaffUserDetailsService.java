package com.smvita.computerseekho.security;

import com.smvita.computerseekho.entity.Staff;
import com.smvita.computerseekho.repository.StaffRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.core.userdetails.User;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.core.userdetails.UserDetailsService;
import org.springframework.security.core.userdetails.UsernameNotFoundException;
import org.springframework.stereotype.Service;

import java.util.List;

/**
 * Bridges the `staff` table to Spring Security. Staff.Role becomes a
 * ROLE_-prefixed authority (Counselor -> ROLE_COUNSELOR) so SecurityConfig
 * can use the standard hasAnyRole(...) matchers.
 *
 * An inactive staff member is loaded as a disabled account rather than
 * being hidden, so deactivating someone in Table Maintenance immediately
 * locks them out instead of silently reporting "bad credentials".
 */
@Service
@RequiredArgsConstructor
public class StaffUserDetailsService implements UserDetailsService {

    private final StaffRepository staffRepository;

    public static String authorityFor(Staff.Role role) {
        return "ROLE_" + role.name().toUpperCase();
    }

    @Override
    public UserDetails loadUserByUsername(String username) throws UsernameNotFoundException {
        Staff staff = staffRepository.findByUsername(username)
                .orElseThrow(() -> new UsernameNotFoundException("No staff account for username '" + username + "'"));

        return User.withUsername(staff.getUsername())
                .password(staff.getPasswordHash())
                .authorities(List.of(new SimpleGrantedAuthority(authorityFor(staff.getRole()))))
                .disabled(Boolean.FALSE.equals(staff.getIsActive()))
                .build();
    }
}
