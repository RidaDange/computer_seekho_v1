package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.LoginRequest;
import com.smvita.computerseekho.dto.LoginResponse;
import com.smvita.computerseekho.dto.StaffDto;
import com.smvita.computerseekho.entity.Staff;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.StaffRepository;
import com.smvita.computerseekho.security.JwtService;
import lombok.RequiredArgsConstructor;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.security.authentication.DisabledException;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@RequiredArgsConstructor
@Transactional(readOnly = true)
public class AuthService {

    private static final Logger log = LoggerFactory.getLogger(AuthService.class);

    private final StaffRepository staffRepository;
    private final AuthenticationManager authenticationManager;
    private final JwtService jwtService;

    /**
     * Verifies the credentials through Spring Security's own provider chain,
     * then mints the JWT.
     *
     * Delegating rather than calling passwordEncoder.matches() here is the
     * point: the provider already handles the timing-safe comparison, the
     * unknown-user-looks-like-wrong-password rule, and the disabled-account
     * check, and StaffUserDetailsService is the single place that decides
     * what a staff row means to the security layer. Duplicating that logic
     * in this method is how the two drift apart.
     */
    public LoginResponse login(LoginRequest request) {
        try {
            authenticationManager.authenticate(
                    new UsernamePasswordAuthenticationToken(request.username(), request.password()));
        } catch (DisabledException ex) {
            // Spring's own message is "User is disabled", which tells a
            // deactivated employee nothing about what to do next.
            throw new DisabledException(
                    "This staff account has been deactivated. Contact an administrator.");
        } catch (BadCredentialsException ex) {
            log.warn("Failed login attempt for username '{}'", request.username());
            // Same generic message whether the username is unknown or the
            // password is wrong — telling an attacker which half they got
            // right is free information.
            throw new BadCredentialsException("Invalid username or password");
        }

        // Authenticated, so the row is certain to exist — this reload is for
        // the claims and the profile the UI renders, not another check.
        Staff staff = staffRepository.findByUsername(request.username())
                .orElseThrow(() -> new BadCredentialsException("Invalid username or password"));

        String token = jwtService.generateToken(
                staff.getUsername(), staff.getRole().name(), staff.getStaffId());

        log.info("Staff '{}' ({}) logged in", staff.getUsername(), staff.getRole());
        return new LoginResponse(token, jwtService.getExpirationMs(), toDto(staff));
    }

    public StaffDto currentStaff(String username) {
        Staff staff = staffRepository.findByUsername(username)
                .orElseThrow(() -> new ResourceNotFoundException("Staff account '" + username + "' no longer exists"));
        return toDto(staff);
    }

    private StaffDto toDto(Staff s) {
        return new StaffDto(s.getStaffId(), s.getName(), s.getEmail(), s.getPhone(),
                s.getRole() != null ? s.getRole().name() : null,
                s.getQualification(), s.getExperience(), s.getPhotoUrl(),
                s.getUsername(), s.getIsActive());
    }
}
