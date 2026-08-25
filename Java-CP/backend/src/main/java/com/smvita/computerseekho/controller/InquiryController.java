package com.smvita.computerseekho.controller;

import com.smvita.computerseekho.dto.CloseInquiryRequest;
import com.smvita.computerseekho.dto.InquiryCreateRequest;
import com.smvita.computerseekho.dto.InquiryDto;
import com.smvita.computerseekho.security.JwtService;
import com.smvita.computerseekho.service.InquiryService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/inquiries")
@RequiredArgsConstructor
public class InquiryController {

    private final InquiryService inquiryService;

    /**
     * The public website's enquiry form.
     *
     * No longer anonymous: a visitor signs in with Google first, so the
     * address an enquiry is filed under is one they demonstrably control.
     * Reading the site stays entirely open — only this write is gated.
     *
     * For a signed-in visitor the posted email is discarded and replaced
     * with the token's subject. Locking the field in the UI is a courtesy;
     * a form field is client-side and can be edited, so if the verified
     * identity is to mean anything it has to be the server that decides
     * which address the enquiry belongs to.
     */
    @PostMapping("/public")
    @ResponseStatus(HttpStatus.CREATED)
    public InquiryDto createFromWebsite(@Valid @RequestBody InquiryCreateRequest request,
                                        Authentication authentication) {
        return inquiryService.create(withVerifiedEmail(request, authentication), "Website");
    }

    /**
     * Substitutes the signed-in visitor's verified address for whatever the
     * form carried.
     *
     * Only for visitors. A staff member who happens to hit this endpoint is
     * recording someone else's enquiry, so their own login address is the
     * wrong answer and the submitted one stands.
     */
    private InquiryCreateRequest withVerifiedEmail(InquiryCreateRequest request,
                                                   Authentication authentication) {
        if (authentication == null || !isVisitor(authentication)) {
            return request;
        }
        String verifiedEmail = authentication.getName();
        if (verifiedEmail == null || verifiedEmail.equalsIgnoreCase(request.email())) {
            return request;
        }
        return new InquiryCreateRequest(
                request.courseId(), request.enquirerName(), verifiedEmail,
                request.phone(), request.message(), request.source());
    }

    private boolean isVisitor(Authentication authentication) {
        return authentication.getAuthorities().stream()
                .anyMatch(a -> ("ROLE_" + JwtService.VISITOR_ROLE).equals(a.getAuthority()));
    }

    /** Staff-entered enquiry — the walk-in visitor at the campus desk. */
    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public InquiryDto createByStaff(@Valid @RequestBody InquiryCreateRequest request) {
        return inquiryService.create(request, "Walk-in");
    }

    /**
     * Everything, including closed and converted — for reporting/history.
     *
     * `mine=true` narrows to the signed-in staff member's own leads. The
     * identity comes from the token, never from a request parameter, so
     * nobody can read a colleague's pipeline by editing the URL.
     */
    @GetMapping
    public List<InquiryDto> findAll(@RequestParam(defaultValue = "false") boolean mine,
                                    @AuthenticationPrincipal String username) {
        return inquiryService.findAll(username, mine);
    }

    /** The working list: New + In-Followup only, closed enquiries hidden. */
    @GetMapping("/active")
    public List<InquiryDto> findActive(@RequestParam(defaultValue = "false") boolean mine,
                                       @AuthenticationPrincipal String username) {
        return inquiryService.findActive(username, mine);
    }

    @GetMapping("/{id}")
    public InquiryDto findById(@PathVariable Integer id) {
        return inquiryService.findById(id);
    }

    /** Close with a mandatory reason from the closure_reasons dropdown. */
    @PutMapping("/{id}/close")
    public InquiryDto close(@PathVariable Integer id, @Valid @RequestBody CloseInquiryRequest request) {
        return inquiryService.close(id, request);
    }

    /** Marks the enquiry converted; Day 4's registration flow hangs off this. */
    @PutMapping("/{id}/convert")
    public InquiryDto convert(@PathVariable Integer id) {
        return inquiryService.markConverted(id);
    }
}
