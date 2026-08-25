package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.PlacementRecordDto;
import com.smvita.computerseekho.entity.Enrollment;
import com.smvita.computerseekho.entity.PlacementRecord;
import com.smvita.computerseekho.exception.BusinessRuleException;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.BatchRepository;
import com.smvita.computerseekho.repository.EnrollmentRepository;
import com.smvita.computerseekho.repository.PlacementRecordRepository;
import com.smvita.computerseekho.repository.RecruiterRepository;
import com.smvita.computerseekho.repository.StudentRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Service
@RequiredArgsConstructor
@Transactional
public class PlacementRecordService {

    private final PlacementRecordRepository placementRecordRepository;
    private final StudentRepository studentRepository;
    private final RecruiterRepository recruiterRepository;
    private final BatchRepository batchRepository;
    private final EnrollmentRepository enrollmentRepository;

    @Transactional(readOnly = true)
    public List<PlacementRecordDto> findAll() {
        return placementRecordRepository.findAllByOrderByPlacementDateDescPlacementIdDesc()
                .stream().map(this::toDto).toList();
    }

    @Transactional(readOnly = true)
    public List<PlacementRecordDto> findByBatch(Integer batchId) {
        return placementRecordRepository.findByBatch_BatchId(batchId).stream().map(this::toDto).toList();
    }

    // Recruiter drill-down: click a company logo -> see everyone placed there.
    @Transactional(readOnly = true)
    public List<PlacementRecordDto> findByRecruiter(Integer recruiterId) {
        return placementRecordRepository.findByRecruiter_RecruiterId(recruiterId).stream().map(this::toDto).toList();
    }

    @Transactional(readOnly = true)
    public long countByBatch(Integer batchId) {
        return placementRecordRepository.countByBatch_BatchId(batchId);
    }

    public PlacementRecordDto create(PlacementRecordDto dto) {
        PlacementRecord record = new PlacementRecord();
        applyDto(record, dto, true);
        return toDto(placementRecordRepository.save(record));
    }

    public PlacementRecordDto update(Integer id, PlacementRecordDto dto) {
        PlacementRecord record = placementRecordRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Placement record", id));
        applyDto(record, dto, false);
        return toDto(placementRecordRepository.save(record));
    }

    public void delete(Integer id) {
        placementRecordRepository.delete(placementRecordRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Placement record", id)));
    }

    private void applyDto(PlacementRecord record, PlacementRecordDto dto, boolean isNew) {
        if (dto.studentId() == null) {
            throw new BusinessRuleException("A student is required.");
        }
        if (dto.recruiterId() == null) {
            throw new BusinessRuleException("A recruiter is required.");
        }

        record.setStudent(studentRepository.findById(dto.studentId())
                .orElseThrow(() -> new ResourceNotFoundException("Student", dto.studentId())));
        record.setRecruiter(recruiterRepository.findById(dto.recruiterId())
                .orElseThrow(() -> new ResourceNotFoundException("Recruiter", dto.recruiterId())));

        // Default the batch from the student's enrolment rather than asking
        // for it again — the Batchwise Placement page groups by batch, and a
        // record with a blank batch quietly disappears from it.
        if (dto.batchId() != null) {
            record.setBatch(batchRepository.findById(dto.batchId())
                    .orElseThrow(() -> new ResourceNotFoundException("Batch", dto.batchId())));
        } else if (record.getBatch() == null) {
            enrollmentRepository.findByStudent_StudentIdOrderByEnrollmentIdDesc(dto.studentId())
                    .stream().findFirst()
                    .map(Enrollment::getBatch)
                    .ifPresent(record::setBatch);
        }

        if (isNew && placementRecordRepository.existsByStudent_StudentIdAndRecruiter_RecruiterId(
                dto.studentId(), dto.recruiterId())) {
            throw new BusinessRuleException(
                    "This student is already recorded as placed at " + record.getRecruiter().getCompanyName() + ".");
        }

        record.setPosition(dto.position());
        record.setPackageAmount(dto.packageAmount());
        record.setPlacementDate(dto.placementDate());
        record.setIsFeatured(dto.isFeatured() != null ? dto.isFeatured() : false);
    }

    private PlacementRecordDto toDto(PlacementRecord p) {
        return new PlacementRecordDto(
                p.getPlacementId(),
                p.getStudent().getStudentId(),
                p.getStudent().getFirstName() + " " + p.getStudent().getLastName(),
                p.getStudent().getPhotoUrl(),
                p.getBatch() != null ? p.getBatch().getBatchId() : null,
                p.getBatch() != null ? p.getBatch().getBatchName() : null,
                p.getRecruiter().getRecruiterId(),
                p.getRecruiter().getCompanyName(),
                p.getPosition(),
                p.getPackageAmount(),
                p.getPlacementDate(),
                p.getIsFeatured()
        );
    }
}
