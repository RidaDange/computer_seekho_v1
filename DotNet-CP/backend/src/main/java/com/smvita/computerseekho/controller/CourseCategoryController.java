package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.CourseCategoryDto;
import com.smvita.computerseekho.service.CourseCategoryService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/course-categories")
@RequiredArgsConstructor
public class CourseCategoryController {

    private final CourseCategoryService courseCategoryService;

    @GetMapping
    public List<CourseCategoryDto> findAll() {
        return courseCategoryService.findAll();
    }

    @GetMapping("/active")
    public List<CourseCategoryDto> findActive() {
        return courseCategoryService.findActive();
    }

    @GetMapping("/{id}")
    public CourseCategoryDto findById(@PathVariable Integer id) {
        return courseCategoryService.findById(id);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public CourseCategoryDto create(@Valid @RequestBody CourseCategoryDto dto) {
        return courseCategoryService.create(dto);
    }

    @PutMapping("/{id}")
    public CourseCategoryDto update(@PathVariable Integer id, @Valid @RequestBody CourseCategoryDto dto) {
        return courseCategoryService.update(id, dto);
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable Integer id) {
        courseCategoryService.delete(id);
    }
}
