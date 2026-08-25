package com.smvita.computerseekho.security;

import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import lombok.RequiredArgsConstructor;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.security.core.Authentication;
import org.springframework.security.oauth2.core.user.OAuth2User;
import org.springframework.security.web.authentication.AuthenticationSuccessHandler;
import org.springframework.stereotype.Component;
import org.springframework.web.util.UriComponentsBuilder;

import java.io.IOException;
import java.nio.charset.StandardCharsets;

/**
 * Turns a completed Google sign-in into one of our own JWTs.
 *
 * Spring finishes the Authorization Code exchange and hands over an
 * OAuth2User; from there the visitor is issued the same kind of token a
 * staff member gets, so `JwtAuthenticationFilter` and every authorization
 * rule keep working unchanged. Google is only ever asked "who is this?" —
 * it is never in the path of a subsequent API call.
 *
 * The token goes back to the SPA as a query parameter on a redirect. That
 * is the weak point of this design: a URL can land in browser history, and
 * in a Referer header if the landing page loads anything third-party. It's
 * mitigated by keeping visitor tokens short-lived (one hour) and by the
 * callback route stripping the parameter from the address bar the moment it
 * has read it. Handing it over in a fragment, or in a single-use exchange
 * code, would be the stronger options if this were carrying more privilege
 * than "may submit an enquiry".
 */
@Component
@RequiredArgsConstructor
public class OAuth2LoginSuccessHandler implements AuthenticationSuccessHandler {

    private static final Logger log = LoggerFactory.getLogger(OAuth2LoginSuccessHandler.class);

    private final JwtService jwtService;

    @Value("${app.oauth2.redirect-uri}")
    private String redirectUri;

    @Override
    public void onAuthenticationSuccess(HttpServletRequest request,
                                        HttpServletResponse response,
                                        Authentication authentication) throws IOException {

        OAuth2User user = (OAuth2User) authentication.getPrincipal();
        String email = user.getAttribute("email");
        String name = user.getAttribute("name");

        if (email == null || email.isBlank()) {
            // Every Google account has one, but the scope can be withheld at
            // the consent screen. Without it there is nothing to file an
            // enquiry against, so this is a failure rather than a partial
            // success.
            log.warn("Google sign-in returned no email attribute; rejecting");
            response.sendRedirect(errorUrl("no_email"));
            return;
        }

        // Google marks an address unverified when it hasn't confirmed
        // ownership. Accepting one would defeat the point of the sign-in.
        Boolean emailVerified = user.getAttribute("email_verified");
        if (Boolean.FALSE.equals(emailVerified)) {
            log.warn("Google sign-in for {} has an unverified email; rejecting", email);
            response.sendRedirect(errorUrl("email_unverified"));
            return;
        }

        String token = jwtService.generateVisitorToken(email, name);
        log.info("Visitor '{}' signed in via Google", email);

        String target = UriComponentsBuilder.fromUriString(redirectUri)
                .queryParam("token", token)
                .queryParam("email", encode(email))
                .queryParam("name", encode(name == null ? "" : name))
                .queryParam("expiresInMs", jwtService.getVisitorExpirationMs())
                .build(true)
                .toUriString();

        response.sendRedirect(target);
    }

    private String errorUrl(String reason) {
        return UriComponentsBuilder.fromUriString(redirectUri)
                .queryParam("error", reason)
                .build(true)
                .toUriString();
    }

    /**
     * Names and addresses contain characters — spaces, '+', accents — that
     * change meaning inside a query string. Encoded here, and build(true)
     * above is told the values are already encoded so they aren't escaped a
     * second time.
     */
    private String encode(String value) {
        return java.net.URLEncoder.encode(value, StandardCharsets.UTF_8);
    }
}
