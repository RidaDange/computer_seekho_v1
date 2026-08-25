package com.smvita.computerseekho.security;

import io.jsonwebtoken.Claims;
import io.jsonwebtoken.JwtException;
import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.security.Keys;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import javax.crypto.SecretKey;
import java.nio.charset.StandardCharsets;
import java.util.Date;

/**
 * Issues and validates the JWTs that back every admin request.
 *
 * The token carries the staff username as its subject plus two convenience
 * claims (role, staffId) so the filter can rebuild an Authentication
 * without a database round-trip on every single request — the reason the
 * API can stay genuinely stateless (SessionCreationPolicy.STATELESS).
 */
@Component
public class JwtService {

    /** Role carried by a visitor token. Staff roles come from Staff.Role. */
    public static final String VISITOR_ROLE = "VISITOR";

    private final SecretKey key;
    private final long expirationMs;
    private final long visitorExpirationMs;

    public JwtService(@Value("${app.jwt.secret}") String secret,
                      @Value("${app.jwt.expiration-ms}") long expirationMs,
                      @Value("${app.jwt.visitor-expiration-ms:3600000}") long visitorExpirationMs) {
        this.visitorExpirationMs = visitorExpirationMs;
        // HS256 requires a key of at least 256 bits; a short JWT_SECRET would
        // otherwise fail at runtime on the first login rather than at startup.
        byte[] keyBytes = secret.getBytes(StandardCharsets.UTF_8);
        if (keyBytes.length < 32) {
            throw new IllegalStateException(
                    "app.jwt.secret must be at least 32 characters (256 bits) for HS256; got " + keyBytes.length);
        }
        this.key = Keys.hmacShaKeyFor(keyBytes);
        this.expirationMs = expirationMs;
    }

    public String generateToken(String username, String role, Integer staffId) {
        Date now = new Date();
        return Jwts.builder()
                .subject(username)
                .claim("role", role)
                .claim("staffId", staffId)
                .issuedAt(now)
                .expiration(new Date(now.getTime() + expirationMs))
                .signWith(key)
                .compact();
    }

    /** Returns the token's claims, or null if it's expired, tampered with, or malformed. */
    public Claims parse(String token) {
        try {
            return Jwts.parser()
                    .verifyWith(key)
                    .build()
                    .parseSignedClaims(token)
                    .getPayload();
        } catch (JwtException | IllegalArgumentException ex) {
            return null;
        }
    }

    /**
     * Mints a token for a website visitor who has just signed in with Google.
     *
     * The same signing key and the same filter read it, so nothing
     * downstream needs to know two token formats — a visitor is simply a
     * principal holding ROLE_VISITOR, and the authorization rules treat it
     * like any other role.
     *
     * The subject is the Google-verified email address. That's the whole
     * point of putting sign-in in front of the enquiry form: the address an
     * enquiry is filed under is one the person demonstrably controls, rather
     * than whatever was typed into the box.
     *
     * There is no staffId claim, so a visitor token can never resolve to a
     * staff member even if one of these reached a staff endpoint.
     */
    public String generateVisitorToken(String email, String name) {
        Date now = new Date();
        return Jwts.builder()
                .subject(email)
                .claim("role", VISITOR_ROLE)
                .claim("name", name)
                .issuedAt(now)
                .expiration(new Date(now.getTime() + visitorExpirationMs))
                .signWith(key)
                .compact();
    }

    public long getExpirationMs() {
        return expirationMs;
    }

    public long getVisitorExpirationMs() {
        return visitorExpirationMs;
    }
}
