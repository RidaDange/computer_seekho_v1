package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.FeeBreakdownDto;
import com.smvita.computerseekho.dto.PaymentDto;
import com.smvita.computerseekho.dto.PaymentRequest;
import com.smvita.computerseekho.service.PaymentService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ContentDisposition;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/payments")
@RequiredArgsConstructor
public class PaymentController {

    private final PaymentService paymentService;

    @GetMapping
    public List<PaymentDto> findAll() {
        return paymentService.findAll();
    }

    /** Collect installment 2. */
    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public PaymentDto collect(@Valid @RequestBody PaymentRequest request) {
        return paymentService.collectNextInstallment(request);
    }

    @GetMapping("/enrollments/{enrollmentId}/fees")
    public FeeBreakdownDto feeStatus(@PathVariable Integer enrollmentId) {
        return paymentService.feeStatusFor(enrollmentId);
    }

    /**
     * The receipt PDF, rendered on demand rather than stored — the database
     * stays the single source of truth, and a corrected student name is
     * reflected the next time the receipt is pulled.
     *
     * Content-Disposition is `inline` so the browser opens it in a tab for
     * the person at the counter; the download button is still one click.
     */
    @GetMapping(value = "/{paymentId}/receipt", produces = MediaType.APPLICATION_PDF_VALUE)
    public ResponseEntity<byte[]> receipt(@PathVariable Integer paymentId) {
        byte[] pdf = paymentService.renderReceipt(paymentId);
        String filename = paymentService.receiptFilename(paymentId);

        return ResponseEntity.ok()
                .header(HttpHeaders.CONTENT_DISPOSITION,
                        ContentDisposition.inline().filename(filename).build().toString())
                .contentType(MediaType.APPLICATION_PDF)
                .body(pdf);
    }
}
