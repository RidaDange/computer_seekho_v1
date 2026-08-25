package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.PlacementDriveDto;
import com.smvita.computerseekho.service.PlacementDriveService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/placement-drives")
@RequiredArgsConstructor
public class PlacementDriveController {

    private final PlacementDriveService placementDriveService;

    @GetMapping
    public List<PlacementDriveDto> findAll() {
        return placementDriveService.findAll();
    }

    @GetMapping("/{id}")
    public PlacementDriveDto findById(@PathVariable Integer id) {
        return placementDriveService.findById(id);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public PlacementDriveDto create(@Valid @RequestBody PlacementDriveDto dto) {
        return placementDriveService.create(dto);
    }

    @PutMapping("/{id}")
    public PlacementDriveDto update(@PathVariable Integer id, @Valid @RequestBody PlacementDriveDto dto) {
        return placementDriveService.update(id, dto);
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable Integer id) {
        placementDriveService.delete(id);
    }
}
