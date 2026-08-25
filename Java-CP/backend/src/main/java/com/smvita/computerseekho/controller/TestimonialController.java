package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.TestimonialDto;
import com.smvita.computerseekho.service.TestimonialService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/testimonials")
@RequiredArgsConstructor
public class TestimonialController {

    private final TestimonialService testimonialService;

    @GetMapping
    public List<TestimonialDto> findAll() {
        return testimonialService.findAll();
    }

    @GetMapping("/approved")
    public List<TestimonialDto> findApproved() {
        return testimonialService.findApproved();
    }

    @GetMapping("/{id}")
    public TestimonialDto findById(@PathVariable Integer id) {
        return testimonialService.findById(id);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public TestimonialDto create(@Valid @RequestBody TestimonialDto dto) {
        return testimonialService.create(dto);
    }

    @PutMapping("/{id}")
    public TestimonialDto update(@PathVariable Integer id, @Valid @RequestBody TestimonialDto dto) {
        return testimonialService.update(id, dto);
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable Integer id) {
        testimonialService.delete(id);
    }
}
