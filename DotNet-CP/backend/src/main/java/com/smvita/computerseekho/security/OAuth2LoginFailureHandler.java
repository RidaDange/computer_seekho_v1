package com.smvita.computerseekho.security;

import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.security.core.AuthenticationException;
import org.springframework.security.web.authentication.AuthenticationFailureHandler;
import org.springframework.stereotype.Component;
import org.springframework.web.util.UriComponentsBuilder;

import java.io.IOException;

/**
 * Sends a failed sign-in back to the SPA instead of Spring's default
 * /login?error page, which doesn't exist here — this application has no
 * server-rendered login view, so the default would show the visitor a 404
 * after they cancelled at Google's consent screen.
 *
 * The reason is deliberately not passed through to the URL. A visitor can
 * act on "sign-in didn't complete"; the underlying OAuth2 error code is for
 * the log, where it isn't also a hint to someone probing the endpoint.
 */
@Component
public class OAuth2LoginFailureHandler implements AuthenticationFailureHandler {

    private static final Logger log = LoggerFactory.getLogger(OAuth2LoginFailureHandler.class);

    @Value("${app.oauth2.redirect-uri}")
    private String redirectUri;

    @Override
    public void onAuthenticationFailure(HttpServletRequest request,
                                        HttpServletResponse response,
                                        AuthenticationException exception) throws IOException {

        log.warn("Google sign-in failed: {}", exception.getMessage());

        response.sendRedirect(UriComponentsBuilder.fromUriString(redirectUri)
                .queryParam("error", "signin_failed")
                .build(true)
                .toUriString());
    }
}
