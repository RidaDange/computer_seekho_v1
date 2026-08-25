package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.RecruiterDto;
import com.smvita.computerseekho.entity.Recruiter;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.RecruiterRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Service
@RequiredArgsConstructor
@Transactional
public class RecruiterService {

    private final RecruiterRepository recruiterRepository;

    public List<RecruiterDto> findAll() {
        return recruiterRepository.findAll().stream().map(this::toDto).toList();
    }

    // Used by the public "Our Recruiters" page — active companies only.
    public List<RecruiterDto> findActive() {
        return recruiterRepository.findByIsActiveTrue().stream().map(this::toDto).toList();
    }

    public RecruiterDto findById(Integer id) {
        return toDto(getEntityOrThrow(id));
    }

    public RecruiterDto create(RecruiterDto dto) {
        Recruiter recruiter = new Recruiter();
        applyDto(recruiter, dto);
        return toDto(recruiterRepository.save(recruiter));
    }

    public RecruiterDto update(Integer id, RecruiterDto dto) {
        Recruiter recruiter = getEntityOrThrow(id);
        applyDto(recruiter, dto);
        return toDto(recruiterRepository.save(recruiter));
    }

    public void delete(Integer id) {
        Recruiter recruiter = getEntityOrThrow(id);
        // NOTE: if this recruiter has historical placement_drives/placement_records,
        // the DB's ON DELETE RESTRICT will reject this and GlobalExceptionHandler's
        // DataIntegrityViolationException handler turns it into a clean 409 — by
        // design (see Knowledge Base: recruiters carry placement history, so
        // "retire" via is_active=false instead of deleting where possible).
        recruiterRepository.delete(recruiter);
    }

    private Recruiter getEntityOrThrow(Integer id) {
        return recruiterRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Recruiter", id));
    }

    private void applyDto(Recruiter recruiter, RecruiterDto dto) {
        recruiter.setCompanyName(dto.companyName());
        recruiter.setLogoUrl(dto.logoUrl());
        recruiter.setIsActive(dto.isActive() != null ? dto.isActive() : true);
    }

    private RecruiterDto toDto(Recruiter r) {
        return new RecruiterDto(r.getRecruiterId(), r.getCompanyName(), r.getLogoUrl(), r.getIsActive());
    }
}
