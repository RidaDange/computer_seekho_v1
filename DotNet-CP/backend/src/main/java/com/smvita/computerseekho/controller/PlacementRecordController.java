package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.PlacementRecordDto;
import com.smvita.computerseekho.service.PlacementRecordService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/placement-records")
@RequiredArgsConstructor
public class PlacementRecordController {

    private final PlacementRecordService placementRecordService;

    /** Admin placement-entry screen. */
    @GetMapping
    public List<PlacementRecordDto> findAll() {
        return placementRecordService.findAll();
    }

    // Placement Detail page (BRD §4.1) — students placed from one batch.
    @GetMapping("/by-batch/{batchId}")
    public List<PlacementRecordDto> findByBatch(@PathVariable Integer batchId) {
        return placementRecordService.findByBatch(batchId);
    }

    // Recruiter drill-down (Turn 4 requirement) — click a recruiter logo,
    // see everyone VITA placed at that company.
    @GetMapping("/by-recruiter/{recruiterId}")
    public List<PlacementRecordDto> findByRecruiter(@PathVariable Integer recruiterId) {
        return placementRecordService.findByRecruiter(recruiterId);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public PlacementRecordDto create(@Valid @RequestBody PlacementRecordDto dto) {
        return placementRecordService.create(dto);
    }

    @PutMapping("/{id}")
    public PlacementRecordDto update(@PathVariable Integer id, @Valid @RequestBody PlacementRecordDto dto) {
        return placementRecordService.update(id, dto);
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable Integer id) {
        placementRecordService.delete(id);
    }
}
