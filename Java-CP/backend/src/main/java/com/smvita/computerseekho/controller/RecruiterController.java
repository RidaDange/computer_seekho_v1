package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.RecruiterDto;
import com.smvita.computerseekho.service.RecruiterService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;

/**
 * Reference pattern: every other Table-Maintenance controller
 * (AnnouncementController, ClosureReasonController, CourseCategoryController...)
 * follows this exact same shape — GET list, GET by id, POST, PUT, DELETE,
 * all thin and delegating to the Service layer.
 */
@RestController
@RequestMapping("/recruiters")
@RequiredArgsConstructor
public class RecruiterController {

    private final RecruiterService recruiterService;

    @GetMapping
    public List<RecruiterDto> findAll() {
        return recruiterService.findAll();
    }

    // Public "Our Recruiters" page consumes this — active companies only.
    @GetMapping("/active")
    public List<RecruiterDto> findActive() {
        return recruiterService.findActive();
    }

    @GetMapping("/{id}")
    public RecruiterDto findById(@PathVariable Integer id) {
        return recruiterService.findById(id);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public RecruiterDto create(@Valid @RequestBody RecruiterDto dto) {
        return recruiterService.create(dto);
    }

    @PutMapping("/{id}")
    public RecruiterDto update(@PathVariable Integer id, @Valid @RequestBody RecruiterDto dto) {
        return recruiterService.update(id, dto);
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable Integer id) {
        recruiterService.delete(id);
    }
}
