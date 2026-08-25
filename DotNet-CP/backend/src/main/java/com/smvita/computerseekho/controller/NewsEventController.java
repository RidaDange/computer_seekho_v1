package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.NewsEventDto;
import com.smvita.computerseekho.service.NewsEventService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/news-events")
@RequiredArgsConstructor
public class NewsEventController {

    private final NewsEventService newsEventService;

    @GetMapping
    public List<NewsEventDto> findAll() {
        return newsEventService.findAll();
    }

    @GetMapping("/active")
    public List<NewsEventDto> findActive() {
        return newsEventService.findActive();
    }

    @GetMapping("/{id}")
    public NewsEventDto findById(@PathVariable Integer id) {
        return newsEventService.findById(id);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public NewsEventDto create(@Valid @RequestBody NewsEventDto dto) {
        return newsEventService.create(dto);
    }

    @PutMapping("/{id}")
    public NewsEventDto update(@PathVariable Integer id, @Valid @RequestBody NewsEventDto dto) {
        return newsEventService.update(id, dto);
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable Integer id) {
        newsEventService.delete(id);
    }
}
