package com.smvita.computerseekho.dto;

/**
 * What the Login screen gets back. `staff` is the full StaffDto (which by
 * design carries no password field of any kind), so the admin shell can
 * show who's logged in and gate nav items by role without a second call.
 */
public record LoginResponse(
        String token,
        long expiresInMs,
        StaffDto staff
) {}
