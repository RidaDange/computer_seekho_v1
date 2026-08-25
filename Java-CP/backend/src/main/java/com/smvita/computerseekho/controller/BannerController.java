package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.BannerDto;
import com.smvita.computerseekho.service.BannerService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/banners")
@RequiredArgsConstructor
public class BannerController {

    private final BannerService bannerService;

    @GetMapping
    public List<BannerDto> findAll() {
        return bannerService.findAll();
    }

    @GetMapping("/valid")
    public List<BannerDto> findCurrentlyValid() {
        return bannerService.findCurrentlyValid();
    }

    @GetMapping("/{id}")
    public BannerDto findById(@PathVariable Integer id) {
        return bannerService.findById(id);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public BannerDto create(@Valid @RequestBody BannerDto dto) {
        return bannerService.create(dto);
    }

    @PutMapping("/{id}")
    public BannerDto update(@PathVariable Integer id, @Valid @RequestBody BannerDto dto) {
        return bannerService.update(id, dto);
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable Integer id) {
        bannerService.delete(id);
    }
}
