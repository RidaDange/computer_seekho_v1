package com.smvita.computerseekho.service;

import jakarta.annotation.PostConstruct;
import jakarta.mail.MessagingException;
import jakarta.mail.internet.MimeMessage;
import lombok.RequiredArgsConstructor;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.core.io.ByteArrayResource;
import org.springframework.mail.MailException;
import org.springframework.mail.SimpleMailMessage;
import org.springframework.mail.javamail.JavaMailSender;
import org.springframework.mail.javamail.MimeMessageHelper;
import org.springframework.stereotype.Service;

import java.nio.charset.StandardCharsets;

/**
 * Single place every "Email ... wherever needed" backend requirement routes
 * through — the Get in Touch form, the enquiry-confirmation email, and
 * Day 4's receipt delivery.
 *
 * Failures are logged, not thrown (see sendSafely): a slow or misconfigured
 * mail server must never fail the underlying business action. An enquiry
 * that was captured correctly should not be rolled back because SMTP was
 * having a bad day.
 */
@Service
@RequiredArgsConstructor
public class EmailService {

    private static final Logger log = LoggerFactory.getLogger(EmailService.class);

    private final JavaMailSender mailSender;

    /**
     * Credentials come from the environment, never from a properties file
     * committed to the repo. Blank means "mail isn't set up here", which is
     * a legitimate state for a dev machine.
     */
    @Value("${spring.mail.username:}")
    private String mailUsername;

    /** Defaults to the authenticating account; Gmail rewrites it anyway. */
    @Value("${app.mail.from:}")
    private String fromAddress;

    /** Escape hatch to silence outbound mail without unsetting credentials. */
    @Value("${app.mail.enabled:true}")
    private boolean mailEnabled;

    @PostConstruct
    void reportConfiguration() {
        if (!isConfigured()) {
            log.warn("Email is NOT configured (spring.mail.username is blank or app.mail.enabled=false). "
                    + "Outbound messages will be skipped and logged instead of sent.");
        } else {
            log.info("Email configured — sending as '{}'", effectiveFrom());
        }
    }

    private boolean isConfigured() {
        return mailEnabled && mailUsername != null && !mailUsername.isBlank();
    }

    private String effectiveFrom() {
        return fromAddress != null && !fromAddress.isBlank() ? fromAddress : mailUsername;
    }

    public void send(String to, String subject, String body) {
        SimpleMailMessage message = new SimpleMailMessage();
        message.setFrom(effectiveFrom());
        message.setTo(to);
        message.setSubject(subject);
        message.setText(body);
        mailSender.send(message);
    }

    /**
     * Sends a message with a single file attachment — the registration
     * receipt today, and whatever Day 5 needs after that.
     *
     * Uses MimeMessage rather than SimpleMailMessage because the latter has
     * no concept of attachments. Returns whether it actually went out, so
     * the caller can tell the user "emailed" versus "download it instead"
     * rather than guessing.
     */
    public boolean sendWithAttachment(String to, String subject, String body,
                                      String filename, byte[] attachment) {
        if (to == null || to.isBlank()) {
            log.warn("Skipping email '{}' — no recipient address", subject);
            return false;
        }
        if (!isConfigured()) {
            log.info("Email not configured — would have sent '{}' to {} with {}", subject, to, filename);
            return false;
        }
        try {
            MimeMessage message = mailSender.createMimeMessage();
            MimeMessageHelper helper = new MimeMessageHelper(message, true, StandardCharsets.UTF_8.name());
            helper.setFrom(effectiveFrom());
            helper.setTo(to);
            helper.setSubject(subject);
            helper.setText(body);
            helper.addAttachment(filename, new ByteArrayResource(attachment));
            mailSender.send(message);
            log.info("Email '{}' sent to {} with attachment {}", subject, to, filename);
            return true;
        } catch (MailException | MessagingException ex) {
            log.warn("Email with attachment to {} failed (subject: '{}'): {}", to, subject, ex.getMessage());
            return false;
        }
    }

    /**
     * Same as send(), but never lets a mail problem escape.
     *
     * When mail isn't configured at all, this short-circuits rather than
     * attempting a doomed SMTP connection — otherwise every enquiry would
     * block for the length of the connection timeout before giving up,
     * which reads as "the app is slow" rather than "email isn't set up".
     */
    public void sendSafely(String to, String subject, String body) {
        if (to == null || to.isBlank()) {
            log.warn("Skipping email '{}' — no recipient address", subject);
            return;
        }
        if (!isConfigured()) {
            log.info("Email not configured — would have sent '{}' to {}", subject, to);
            return;
        }
        try {
            send(to, subject, body);
            log.info("Email '{}' sent to {}", subject, to);
        } catch (MailException ex) {
            log.warn("Email to {} failed to send (subject: '{}'): {}", to, subject, ex.getMessage());
        }
    }
}
