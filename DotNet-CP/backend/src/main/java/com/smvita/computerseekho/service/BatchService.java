package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.BatchDetailDto;
import com.smvita.computerseekho.dto.BatchDto;
import com.smvita.computerseekho.entity.Batch;
import com.smvita.computerseekho.entity.Course;
import com.smvita.computerseekho.entity.Enrollment;
import com.smvita.computerseekho.entity.Staff;
import com.smvita.computerseekho.exception.BusinessRuleException;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.BatchRepository;
import com.smvita.computerseekho.repository.CourseRepository;
import com.smvita.computerseekho.repository.EnrollmentRepository;
import com.smvita.computerseekho.repository.PlacementRecordRepository;
import com.smvita.computerseekho.repository.StaffRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

/**
 * Batch Management.
 *
 * KB §9.1: batches are deliberately kept out of the generic Table
 * Maintenance grid, because current_count and status are system-driven
 * rather than hand-edited. This service is what that decision buys —
 * current_count is always recomputed from enrollments, never accepted from
 * a form.
 */
@Service
@RequiredArgsConstructor
@Transactional
public class BatchService {

    private final BatchRepository batchRepository;
    private final PlacementRecordRepository placementRecordRepository;
    private final EnrollmentRepository enrollmentRepository;
    private final CourseRepository courseRepository;
    private final StaffRepository staffRepository;

    @Transactional(readOnly = true)
    public List<BatchDto> findAll() {
        return batchRepository.findAll().stream().map(this::toDto).toList();
    }

    // Batchwise Placement page — completed batches only, each with its
    // resolved placement count for the "X/Y placed" display.
    @Transactional(readOnly = true)
    public List<BatchDto> findCompletedForPlacementDisplay() {
        return batchRepository.findByStatusAndIsActiveTrue(Batch.Status.Completed)
                .stream().map(this::toDto).toList();
    }

    @Transactional(readOnly = true)
    public BatchDto findById(Integer id) {
        return toDto(getEntityOrThrow(id));
    }

    /** The full record behind the Batch Management screen. */
    @Transactional(readOnly = true)
    public List<BatchDetailDto> findAllDetailed() {
        return batchRepository.findAllByOrderByStartDateDescBatchIdDesc()
                .stream().map(this::toDetailDto).toList();
    }

    @Transactional(readOnly = true)
    public BatchDetailDto findDetailById(Integer id) {
        return toDetailDto(getEntityOrThrow(id));
    }

    public BatchDetailDto create(BatchDetailDto dto) {
        Batch batch = new Batch();
        applyDto(batch, dto);
        return toDetailDto(batchRepository.save(batch));
    }

    public BatchDetailDto update(Integer id, BatchDetailDto dto) {
        Batch batch = getEntityOrThrow(id);

        long enrolled = enrollmentRepository.countByBatch_BatchIdAndStatusNot(id, Enrollment.Status.Dropped);
        if (dto.capacity() != null && dto.capacity() < enrolled) {
            throw new BusinessRuleException(
                    "Capacity can't drop below the %d students already enrolled.".formatted(enrolled));
        }

        applyDto(batch, dto);
        return toDetailDto(batchRepository.save(batch));
    }

    public void delete(Integer id) {
        Batch batch = getEntityOrThrow(id);
        long enrolled = enrollmentRepository.countByBatch_BatchIdAndStatusNot(id, Enrollment.Status.Dropped);
        if (enrolled > 0) {
            // The FK is ON DELETE RESTRICT, so this would fail at the DB
            // anyway — catching it here gives a message that explains why.
            throw new BusinessRuleException(
                    "%s has %d enrolled students. Mark it Cancelled or Completed instead of deleting it."
                            .formatted(batch.getBatchName(), enrolled));
        }
        batchRepository.delete(batch);
    }

    private Batch getEntityOrThrow(Integer id) {
        return batchRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Batch", id));
    }

    private void applyDto(Batch batch, BatchDetailDto dto) {
        Course course = courseRepository.findById(dto.courseId())
                .orElseThrow(() -> new ResourceNotFoundException("Course", dto.courseId()));
        Staff staff = staffRepository.findById(dto.staffId())
                .orElseThrow(() -> new ResourceNotFoundException("Staff", dto.staffId()));

        if (dto.startDate() != null && dto.endDate() != null && dto.endDate().isBefore(dto.startDate())) {
            throw new BusinessRuleException("The end date can't be before the start date.");
        }

        batch.setCourse(course);
        batch.setStaff(staff);
        batch.setBatchName(dto.batchName().trim());
        batch.setAcademicYear(dto.academicYear());
        batch.setStartDate(dto.startDate());
        batch.setEndDate(dto.endDate());
        batch.setPresentationDate(dto.presentationDate());
        batch.setTiming(dto.timing());
        batch.setCapacity(dto.capacity());
        if (dto.status() != null && !dto.status().isBlank()) {
            batch.setStatus(parseStatus(dto.status()));
        }
        batch.setIsActive(dto.isActive() != null ? dto.isActive() : true);

        // Never taken from the DTO — recomputed from the roster instead.
        if (batch.getBatchId() != null) {
            batch.setCurrentCount((int) enrollmentRepository
                    .countByBatch_BatchIdAndStatusNot(batch.getBatchId(), Enrollment.Status.Dropped));
        }
    }

    private Batch.Status parseStatus(String raw) {
        for (Batch.Status s : Batch.Status.values()) {
            if (s.name().equalsIgnoreCase(raw)) return s;
        }
        throw new BusinessRuleException("Unknown batch status: '" + raw + "'");
    }

    private BatchDetailDto toDetailDto(Batch b) {
        return new BatchDetailDto(
                b.getBatchId(),
                b.getCourse().getCourseId(), b.getCourse().getName(),
                b.getStaff().getStaffId(), b.getStaff().getName(),
                b.getBatchName(), b.getAcademicYear(),
                b.getStartDate(), b.getEndDate(), b.getPresentationDate(),
                b.getTiming(), b.getCapacity(), b.getCurrentCount(),
                b.getStatus().name(), b.getIsActive());
    }

    private BatchDto toDto(Batch b) {
        long placedCount = placementRecordRepository.countByBatch_BatchId(b.getBatchId());
        return new BatchDto(
                b.getBatchId(),
                b.getBatchName(),
                b.getCourse().getCourseId(),
                b.getCourse().getName(),
                b.getCourse().getCategory().getCategoryId(),
                b.getCourse().getCategory().getName(),
                b.getAcademicYear(),
                b.getCapacity(),
                b.getCurrentCount(),
                b.getStatus().name(),
                b.getIsActive(),
                placedCount
        );
    }
}
