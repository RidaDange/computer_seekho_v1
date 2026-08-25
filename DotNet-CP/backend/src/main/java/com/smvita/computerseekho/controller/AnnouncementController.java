package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.AnnouncementDto;
import com.smvita.computerseekho.service.AnnouncementService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/announcements")
@RequiredArgsConstructor
public class AnnouncementController {

    private final AnnouncementService announcementService;

    @GetMapping
    public List<AnnouncementDto> findAll() {
        return announcementService.findAll();
    }

    // Home page ticker consumes this.
    @GetMapping("/valid")
    public List<AnnouncementDto> findCurrentlyValid() {
        return announcementService.findCurrentlyValid();
    }

    @GetMapping("/{id}")
    public AnnouncementDto findById(@PathVariable Integer id) {
        return announcementService.findById(id);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public AnnouncementDto create(@Valid @RequestBody AnnouncementDto dto) {
        return announcementService.create(dto);
    }

    @PutMapping("/{id}")
    public AnnouncementDto update(@PathVariable Integer id, @Valid @RequestBody AnnouncementDto dto) {
        return announcementService.update(id, dto);
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable Integer id) {
        announcementService.delete(id);
    }
}
