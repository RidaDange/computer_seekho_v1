package com.smvita.computerseekho.config;

import com.smvita.computerseekho.security.JwtAuthenticationFilter;
import com.smvita.computerseekho.security.OAuth2LoginFailureHandler;
import com.smvita.computerseekho.security.OAuth2LoginSuccessHandler;
import com.smvita.computerseekho.security.StaffUserDetailsService;
import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.core.annotation.Order;
import org.springframework.http.HttpMethod;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.ProviderManager;
import org.springframework.security.authentication.dao.DaoAuthenticationProvider;
import org.springframework.security.config.Customizer;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;

/**
 * Day 3: replaces the Day-1 permitAll() placeholder with a real stateless
 * JWT chain.
 *
 * The rule set mirrors how SMVITA actually works rather than a generic
 * admin/user split:
 *
 *   - Public site reads (courses, faculty, gallery, recruiters, placements,
 *     banners, announcements...) need no token at all — the website has to
 *     work for anonymous visitors.
 *   - The Get in Touch form is open for the same reason.
 *   - A visitor's enquiry submission (POST /inquiries/public) is the
 *     exception: it needs an identity, obtained through Google on the
 *     OAuth chain below, so an enquiry is filed against a verified address
 *     rather than a typed one.
 *   - The CRM — enquiries and follow-ups — is the counselor's daily job, so
 *     Counselor and Receptionist get full access alongside Admin/Manager.
 *   - Staff records and every master-table write stay with Admin/Manager.
 *   - Faculty land in the catch-all: authenticated, so they can read admin
 *     screens, but no writes.
 *
 * Path patterns here are relative to the /api context-path, which the
 * servlet container strips before Spring Security sees the request.
 */
@Configuration
@EnableWebSecurity
@RequiredArgsConstructor
public class SecurityConfig {

    private final JwtAuthenticationFilter jwtAuthenticationFilter;
    private final OAuth2LoginSuccessHandler oAuth2LoginSuccessHandler;
    private final OAuth2LoginFailureHandler oAuth2LoginFailureHandler;

    /** Endpoints the public website reads without ever logging in. */
    private static final String[] PUBLIC_READ_PATHS = {
            "/courses/**",
            "/course-categories/**",
            "/closure-reasons/**",
            "/banners/**",
            "/announcements/**",
            "/testimonials/**",
            "/news-events/**",
            "/gallery-images/**",
            "/recruiters/**",
            "/placement-records/**",
            "/staff/by-role/**",   // the public Faculty page — read-only, active staff

            // Batch reads are listed individually rather than as /batches/**:
            // the public site needs the placement listings and the photo
            // album, but /batches/detailed carries staff assignments and
            // capacity, which is internal.
            "/batches",
            "/batches/completed-for-placement",
            "/batches/*",
            "/batches/*/album",
            "/batch-albums"   // the public Batch Albums strip on Campus Life
    };

    private static final String[] CRM_ROLES = { "ADMIN", "MANAGER", "COUNSELOR", "RECEPTIONIST" };
    private static final String[] MASTER_DATA_ROLES = { "ADMIN", "MANAGER" };

    /**
     * The Google sign-in handshake, on its own chain ahead of the API.
     *
     * It needs a session and the API must not have one. Between redirecting
     * the visitor to Google and receiving them back at the callback, Spring
     * has to remember the authorization request — the PKCE verifier and the
     * `state` value that proves the response belongs to a request we
     * actually made. That is server-side state by definition, and dropping
     * it is what turns the callback into an open door for a forged code.
     *
     * Session policy is a property of a whole chain, so rather than
     * weakening the API to STATELESS-in-name-only, the two concerns get two
     * chains. Only these two paths are session-backed; every business
     * endpoint below still refuses to create one.
     *
     * CSRF is disabled here for the same reason it is on the API: there is
     * no browser form posting to these paths. The `state` parameter is what
     * protects the callback, and Spring validates it for us.
     */
    @Bean
    @Order(1)
    public SecurityFilterChain oauth2LoginChain(HttpSecurity http) throws Exception {
        http
                .securityMatcher("/oauth2/**", "/login/oauth2/**")
                .cors(Customizer.withDefaults())
                .csrf(csrf -> csrf.disable())
                .sessionManagement(sm -> sm.sessionCreationPolicy(SessionCreationPolicy.IF_REQUIRED))
                .authorizeHttpRequests(auth -> auth.anyRequest().permitAll())
                .oauth2Login(oauth -> oauth
                        .successHandler(oAuth2LoginSuccessHandler)
                        .failureHandler(oAuth2LoginFailureHandler));

        return http.build();
    }

    @Bean
    @Order(2)
    public SecurityFilterChain filterChain(HttpSecurity http) throws Exception {
        http
                // CORS must be handled inside the security chain: preflight
                // OPTIONS requests never reach Spring MVC's CorsRegistry,
                // because the filter chain runs first and would reject them.
                .cors(Customizer.withDefaults())
                .csrf(csrf -> csrf.disable())
                .sessionManagement(sm -> sm.sessionCreationPolicy(SessionCreationPolicy.STATELESS))
                .authorizeHttpRequests(auth -> auth
                        // --- always open ---
                        .requestMatchers(HttpMethod.OPTIONS, "/**").permitAll()
                        .requestMatchers("/auth/login").permitAll()
                        .requestMatchers("/actuator/health", "/actuator/info").permitAll()

                        // --- internal batch views, claimed before the public
                        //     rules below: /batches/* would otherwise match
                        //     /batches/detailed and expose staff assignments
                        //     and capacity to anonymous callers ---
                        .requestMatchers(HttpMethod.GET, "/batches/detailed", "/batches/*/detail")
                        .authenticated()

                        // --- the public website ---
                        .requestMatchers(HttpMethod.GET, PUBLIC_READ_PATHS).permitAll()
                        .requestMatchers(HttpMethod.POST, "/contact-messages").permitAll()

                        // The enquiry form is the one public write that now
                        // requires an identity — a visitor signs in with
                        // Google first, and the enquiry is filed against the
                        // address Google verified rather than whatever was
                        // typed in. Reading the site stays entirely open.
                        //
                        // authenticated() rather than hasRole("VISITOR"): a
                        // staff member browsing the public site already holds
                        // a staff token, and bouncing them to Google to
                        // re-identify as a visitor would be theatre.
                        .requestMatchers(HttpMethod.POST, "/inquiries/public").authenticated()

                        // Uploaded images are read by the public site — batch
                        // albums, faculty portraits, placement photos — so
                        // fetching one needs no token. Storing one does, and
                        // this rule has to precede the blanket POST rule
                        // below or the registration desk (Counselor,
                        // Receptionist) couldn't attach a student's photo.
                        .requestMatchers(HttpMethod.GET, "/uploads/**").permitAll()
                        .requestMatchers(HttpMethod.POST, "/uploads").hasAnyRole(CRM_ROLES)

                        // --- the CRM core, and the registration desk ---
                        // Registration, student records and fee collection are
                        // front-desk work, so Counselor and Receptionist need
                        // write access here — not just Admin/Manager.
                        .requestMatchers("/inquiries/**", "/followups/**",
                                "/registrations/**", "/students/**", "/payments/**")
                        .hasAnyRole(CRM_ROLES)

                        // --- staff records + every master-table write ---
                        .requestMatchers("/staff/**").hasAnyRole(MASTER_DATA_ROLES)
                        .requestMatchers(HttpMethod.POST, "/**").hasAnyRole(MASTER_DATA_ROLES)
                        .requestMatchers(HttpMethod.PUT, "/**").hasAnyRole(MASTER_DATA_ROLES)
                        .requestMatchers(HttpMethod.DELETE, "/**").hasAnyRole(MASTER_DATA_ROLES)

                        // --- everything else: any logged-in staff member,
                        //     which is what makes Faculty read-only ---
                        .anyRequest().authenticated()
                )
                .addFilterBefore(jwtAuthenticationFilter, UsernamePasswordAuthenticationFilter.class);

        return http.build();
    }

    @Bean
    public PasswordEncoder passwordEncoder() {
        return new BCryptPasswordEncoder();
    }

    /**
     * The authentication pipeline behind POST /auth/login.
     *
     * Built explicitly from a DaoAuthenticationProvider rather than pulled
     * off AuthenticationConfiguration: the provider needs the PasswordEncoder
     * declared in this same class, and asking the framework to resolve that
     * for us is the usual source of a bean cycle here. Two lines of wiring
     * avoids it and makes the chain readable.
     *
     * Using the standard provider — instead of comparing hashes by hand —
     * also means the account-status checks come for free: a staff row with
     * is_active = 0 is loaded as a disabled UserDetails and rejected before
     * the password is ever considered.
     */
    @Bean
    public AuthenticationManager authenticationManager(StaffUserDetailsService userDetailsService,
                                                       PasswordEncoder passwordEncoder) {
        DaoAuthenticationProvider provider = new DaoAuthenticationProvider();
        provider.setUserDetailsService(userDetailsService);
        provider.setPasswordEncoder(passwordEncoder);

        // Default behaviour, stated explicitly because the security of the
        // login endpoint depends on it: an unknown username surfaces as
        // BadCredentials, identical to a wrong password, so the response
        // can't be used to enumerate who has an account.
        provider.setHideUserNotFoundExceptions(true);

        return new ProviderManager(provider);
    }
}
