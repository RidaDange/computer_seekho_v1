package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.ContactMessageDto;
import com.smvita.computerseekho.service.ContactMessageService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/contact-messages")
@RequiredArgsConstructor
public class ContactMessageController {

    private final ContactMessageService contactMessageService;

    // Public "Get in Touch" form submits here.
    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public ContactMessageDto submit(@Valid @RequestBody ContactMessageDto dto) {
        return contactMessageService.submit(dto);
    }

    // Admin-side inbox view (not gated yet — real role gating arrives with
    // Day 3's JWT filter chain).
    @GetMapping
    public List<ContactMessageDto> findAll() {
        return contactMessageService.findAll();
    }

    @GetMapping("/{id}")
    public ContactMessageDto findById(@PathVariable Integer id) {
        return contactMessageService.findById(id);
    }

    @PutMapping("/{id}/mark-read")
    public void markRead(@PathVariable Integer id) {
        contactMessageService.markRead(id);
    }
}
