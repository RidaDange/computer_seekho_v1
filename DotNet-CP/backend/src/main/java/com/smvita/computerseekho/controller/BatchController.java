package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.BatchDetailDto;
import com.smvita.computerseekho.dto.BatchDto;
import com.smvita.computerseekho.service.BatchService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/batches")
@RequiredArgsConstructor
public class BatchController {

    private final BatchService batchService;

    @GetMapping
    public List<BatchDto> findAll() {
        return batchService.findAll();
    }

    // Batchwise Placement page consumes this.
    @GetMapping("/completed-for-placement")
    public List<BatchDto> findCompletedForPlacementDisplay() {
        return batchService.findCompletedForPlacementDisplay();
    }

    /** Full records for the dedicated Batch Management screen. */
    @GetMapping("/detailed")
    public List<BatchDetailDto> findAllDetailed() {
        return batchService.findAllDetailed();
    }

    @GetMapping("/{id}")
    public BatchDto findById(@PathVariable Integer id) {
        return batchService.findById(id);
    }

    @GetMapping("/{id}/detail")
    public BatchDetailDto findDetailById(@PathVariable Integer id) {
        return batchService.findDetailById(id);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public BatchDetailDto create(@Valid @RequestBody BatchDetailDto dto) {
        return batchService.create(dto);
    }

    @PutMapping("/{id}")
    public BatchDetailDto update(@PathVariable Integer id, @Valid @RequestBody BatchDetailDto dto) {
        return batchService.update(id, dto);
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable Integer id) {
        batchService.delete(id);
    }
}
