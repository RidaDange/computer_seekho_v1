package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.FollowupDto;
import com.smvita.computerseekho.dto.FollowupLogRequest;
import com.smvita.computerseekho.service.FollowupService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.format.annotation.DateTimeFormat;
import org.springframework.http.HttpStatus;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.*;

import java.time.LocalDate;
import java.util.List;

@RestController
@RequestMapping("/followups")
@RequiredArgsConstructor
public class FollowupController {

    private final FollowupService followupService;

    /**
     * The admin panel's landing list: due today or overdue, open enquiries
     * only. `mine=true` narrows it to the signed-in counselor's own leads,
     * resolved from the token rather than a request parameter.
     */
    @GetMapping("/due")
    public List<FollowupDto> findDue(@RequestParam(defaultValue = "false") boolean mine,
                                     @AuthenticationPrincipal String username) {
        return followupService.findDue(username, mine);
    }

    /** Already booked, but dated after today — deliberately not in /due. */
    @GetMapping("/upcoming")
    public List<FollowupDto> findUpcoming(@RequestParam(defaultValue = "false") boolean mine,
                                          @AuthenticationPrincipal String username) {
        return followupService.findUpcoming(username, mine);
    }

    @GetMapping("/by-inquiry/{inquiryId}")
    public List<FollowupDto> findByInquiry(@PathVariable Integer inquiryId) {
        return followupService.findByInquiry(inquiryId);
    }

    @PutMapping("/{id}/log")
    public FollowupDto log(@PathVariable Integer id, @Valid @RequestBody FollowupLogRequest request) {
        return followupService.logOutcome(id, request);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public FollowupDto schedule(@RequestParam Integer inquiryId,
                                @RequestParam Integer staffId,
                                @RequestParam @DateTimeFormat(iso = DateTimeFormat.ISO.DATE) LocalDate date) {
        return followupService.schedule(inquiryId, staffId, date);
    }
}
