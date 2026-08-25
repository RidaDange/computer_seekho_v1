package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.ClosureReasonDto;
import com.smvita.computerseekho.service.ClosureReasonService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/closure-reasons")
@RequiredArgsConstructor
public class ClosureReasonController {

    private final ClosureReasonService closureReasonService;

    @GetMapping
    public List<ClosureReasonDto> findAll() {
        return closureReasonService.findAll();
    }

    @GetMapping("/active")
    public List<ClosureReasonDto> findActive() {
        return closureReasonService.findActive();
    }

    @GetMapping("/{id}")
    public ClosureReasonDto findById(@PathVariable Integer id) {
        return closureReasonService.findById(id);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public ClosureReasonDto create(@Valid @RequestBody ClosureReasonDto dto) {
        return closureReasonService.create(dto);
    }

    @PutMapping("/{id}")
    public ClosureReasonDto update(@PathVariable Integer id, @Valid @RequestBody ClosureReasonDto dto) {
        return closureReasonService.update(id, dto);
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable Integer id) {
        closureReasonService.delete(id);
    }
}
