package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.StaffCreationResult;
import com.smvita.computerseekho.dto.StaffDto;
import com.smvita.computerseekho.entity.Staff;
import com.smvita.computerseekho.exception.BusinessRuleException;
import com.smvita.computerseekho.service.StaffService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.Arrays;
import java.util.List;

@RestController
@RequestMapping("/staff")
@RequiredArgsConstructor
public class StaffController {

    private final StaffService staffService;

    @GetMapping
    public List<StaffDto> findAll() {
        return staffService.findAll();
    }

    // e.g. GET /staff/by-role/Counselor — used by the auto-assignment logic
    // (Day 3), the public Faculty page, and "who can be primary faculty"
    // dropdowns.
    //
    // Bound as a String and parsed here rather than as @PathVariable
    // Staff.Role: Spring's built-in enum conversion is case-sensitive, so
    // /by-role/faculty threw a conversion error that the global handler
    // turned into an opaque 500. Parsing explicitly gives a clear message
    // and accepts any casing.
    @GetMapping("/by-role/{role}")
    public List<StaffDto> findActiveByRole(@PathVariable String role) {
        return staffService.findActiveByRole(parseRole(role));
    }

    private Staff.Role parseRole(String raw) {
        for (Staff.Role r : Staff.Role.values()) {
            if (r.name().equalsIgnoreCase(raw)) {
                return r;
            }
        }
        throw new BusinessRuleException("Unknown staff role: '" + raw + "'. Valid roles: "
                + Arrays.toString(Staff.Role.values()));
    }

    @GetMapping("/{id}")
    public StaffDto findById(@PathVariable Integer id) {
        return staffService.findById(id);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public StaffCreationResult create(@Valid @RequestBody StaffDto dto) {
        return staffService.create(dto);
    }

    @PutMapping("/{id}")
    public StaffDto update(@PathVariable Integer id, @Valid @RequestBody StaffDto dto) {
        return staffService.update(id, dto);
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable Integer id) {
        staffService.delete(id);
    }
}
