package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.LoginRequest;
import com.smvita.computerseekho.dto.LoginResponse;
import com.smvita.computerseekho.dto.StaffDto;
import com.smvita.computerseekho.service.AuthService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/auth")
@RequiredArgsConstructor
public class AuthController {

    private final AuthService authService;

    @PostMapping("/login")
    public LoginResponse login(@Valid @RequestBody LoginRequest request) {
        return authService.login(request);
    }

    /**
     * Lets the frontend re-hydrate the logged-in staff member from a token
     * that survived a page refresh in localStorage, without making the user
     * log in again. Returns 401 via the security chain if the token is gone
     * or expired.
     */
    @GetMapping("/me")
    public StaffDto me(@AuthenticationPrincipal String username) {
        return authService.currentStaff(username);
    }
}
