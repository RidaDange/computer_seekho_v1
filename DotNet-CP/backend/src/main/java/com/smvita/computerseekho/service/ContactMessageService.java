package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.ContactMessageDto;
import com.smvita.computerseekho.entity.ContactMessage;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.ContactMessageRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Service
@RequiredArgsConstructor
@Transactional
public class ContactMessageService {

    private final ContactMessageRepository contactMessageRepository;
    private final EmailService emailService;

    // BRD: "the data will be saved in database and appropriate mail will
    // be sent to predefined mail-id". Distinct from the course-enquiry
    // confirmation flow (Day 3), which emails the enquirer, not the institute.
    @Value("${app.contact-notification-email:training@vita.com}")
    private String notificationEmail;

    public List<ContactMessageDto> findAll() {
        return contactMessageRepository.findAll().stream().map(this::toDto).toList();
    }

    public ContactMessageDto findById(Integer id) {
        return toDto(getEntityOrThrow(id));
    }

    public ContactMessageDto submit(ContactMessageDto dto) {
        ContactMessage message = new ContactMessage();
        message.setName(dto.name());
        message.setEmail(dto.email());
        message.setMessage(dto.message());
        ContactMessage saved = contactMessageRepository.save(message);

        emailService.sendSafely(
                notificationEmail,
                "New Get in Touch message from " + dto.name(),
                "Name: " + dto.name() + "\nEmail: " + dto.email() + "\n\nMessage:\n" + dto.message()
        );

        return toDto(saved);
    }

    public void markRead(Integer id) {
        ContactMessage message = getEntityOrThrow(id);
        message.setIsRead(true);
        contactMessageRepository.save(message);
    }

    private ContactMessage getEntityOrThrow(Integer id) {
        return contactMessageRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Contact Message", id));
    }

    private ContactMessageDto toDto(ContactMessage m) {
        return new ContactMessageDto(m.getMessageId(), m.getName(), m.getEmail(),
                m.getMessage(), m.getIsRead(), m.getCreatedAt());
    }
}
