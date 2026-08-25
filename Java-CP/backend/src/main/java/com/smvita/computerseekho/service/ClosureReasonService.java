package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.ClosureReasonDto;
import com.smvita.computerseekho.entity.ClosureReason;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.ClosureReasonRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Service
@RequiredArgsConstructor
@Transactional
public class ClosureReasonService {

    private final ClosureReasonRepository closureReasonRepository;

    public List<ClosureReasonDto> findAll() {
        return closureReasonRepository.findAll().stream().map(this::toDto).toList();
    }

    // Close-Enquiry screen's dropdown consumes this.
    public List<ClosureReasonDto> findActive() {
        return closureReasonRepository.findByIsActiveTrue().stream().map(this::toDto).toList();
    }

    public ClosureReasonDto findById(Integer id) {
        return toDto(getEntityOrThrow(id));
    }

    public ClosureReasonDto create(ClosureReasonDto dto) {
        ClosureReason reason = new ClosureReason();
        applyDto(reason, dto);
        return toDto(closureReasonRepository.save(reason));
    }

    public ClosureReasonDto update(Integer id, ClosureReasonDto dto) {
        ClosureReason reason = getEntityOrThrow(id);
        applyDto(reason, dto);
        return toDto(closureReasonRepository.save(reason));
    }

    public void delete(Integer id) {
        // If this reason has already been used on any inquiry, the DB's
        // ON DELETE RESTRICT rejects the delete — GlobalExceptionHandler
        // turns that into a clean 409. Prefer deactivating (is_active=false)
        // over deleting a reason with history.
        closureReasonRepository.delete(getEntityOrThrow(id));
    }

    private ClosureReason getEntityOrThrow(Integer id) {
        return closureReasonRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Closure Reason", id));
    }

    private void applyDto(ClosureReason reason, ClosureReasonDto dto) {
        reason.setReasonText(dto.reasonText());
        reason.setIsActive(dto.isActive() != null ? dto.isActive() : true);
    }

    private ClosureReasonDto toDto(ClosureReason reason) {
        return new ClosureReasonDto(reason.getReasonId(), reason.getReasonText(), reason.getIsActive());
    }
}
