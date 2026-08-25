package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.CourseDto;
import com.smvita.computerseekho.service.CourseService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/courses")
@RequiredArgsConstructor
public class CourseController {

    private final CourseService courseService;

    @GetMapping
    public List<CourseDto> findAll() {
        return courseService.findAll();
    }

    // Home page "Courses" dropdown consumes this — active courses only.
    @GetMapping("/active")
    public List<CourseDto> findActive() {
        return courseService.findActive();
    }

    // Course Detail page — includes resolved category + primary faculty.
    @GetMapping("/{id}")
    public CourseDto findById(@PathVariable Integer id) {
        return courseService.findById(id);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public CourseDto create(@Valid @RequestBody CourseDto dto) {
        return courseService.create(dto);
    }

    @PutMapping("/{id}")
    public CourseDto update(@PathVariable Integer id, @Valid @RequestBody CourseDto dto) {
        return courseService.update(id, dto);
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable Integer id) {
        courseService.delete(id);
    }

    // Dedicated endpoint for the is_primary faculty assignment — kept
    // separate from the main course update since it's really managing the
    // course_staff junction, not a plain Course field.
    @PutMapping("/{id}/primary-faculty/{staffId}")
    public void setPrimaryFaculty(@PathVariable Integer id, @PathVariable Integer staffId) {
        courseService.setPrimaryFaculty(id, staffId);
    }
}
