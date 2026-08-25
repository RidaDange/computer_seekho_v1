package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.PaymentDto;
import com.smvita.computerseekho.dto.StudentDto;
import com.smvita.computerseekho.service.PaymentService;
import com.smvita.computerseekho.service.StudentService;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/students")
@RequiredArgsConstructor
public class StudentController {

    private final StudentService studentService;
    private final PaymentService paymentService;

    @GetMapping
    public List<StudentDto> findAll() {
        return studentService.findAll();
    }

    @GetMapping("/{id}")
    public StudentDto findById(@PathVariable Integer id) {
        return studentService.findById(id);
    }

    /** Batch roster — who's enrolled in this batch. */
    @GetMapping("/by-batch/{batchId}")
    public List<StudentDto> findByBatch(@PathVariable Integer batchId) {
        return studentService.findByBatch(batchId);
    }

    @GetMapping("/{id}/payments")
    public List<PaymentDto> payments(@PathVariable Integer id) {
        return paymentService.findByStudent(id);
    }
}
