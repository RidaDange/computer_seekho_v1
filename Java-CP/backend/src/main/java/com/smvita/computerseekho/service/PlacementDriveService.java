package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.PlacementDriveDto;
import com.smvita.computerseekho.entity.PlacementDrive;
import com.smvita.computerseekho.exception.BusinessRuleException;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.CourseRepository;
import com.smvita.computerseekho.repository.PlacementDriveRepository;
import com.smvita.computerseekho.repository.RecruiterRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

/**
 * Placement drives — the recruiter visit, as distinct from its outcomes.
 * A drive is "TCS came on the 12th with 40 openings"; a placement_record is
 * "Rahul got one of them".
 */
@Service
@RequiredArgsConstructor
@Transactional
public class PlacementDriveService {

    private final PlacementDriveRepository driveRepository;
    private final RecruiterRepository recruiterRepository;
    private final CourseRepository courseRepository;

    @Transactional(readOnly = true)
    public List<PlacementDriveDto> findAll() {
        return driveRepository.findAllByOrderByDriveDateDescDriveIdDesc()
                .stream().map(this::toDto).toList();
    }

    @Transactional(readOnly = true)
    public PlacementDriveDto findById(Integer id) {
        return toDto(getEntityOrThrow(id));
    }

    public PlacementDriveDto create(PlacementDriveDto dto) {
        PlacementDrive drive = new PlacementDrive();
        applyDto(drive, dto);
        return toDto(driveRepository.save(drive));
    }

    public PlacementDriveDto update(Integer id, PlacementDriveDto dto) {
        PlacementDrive drive = getEntityOrThrow(id);
        applyDto(drive, dto);
        return toDto(driveRepository.save(drive));
    }

    public void delete(Integer id) {
        driveRepository.delete(getEntityOrThrow(id));
    }

    private PlacementDrive getEntityOrThrow(Integer id) {
        return driveRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Placement drive", id));
    }

    private void applyDto(PlacementDrive drive, PlacementDriveDto dto) {
        drive.setRecruiter(recruiterRepository.findById(dto.recruiterId())
                .orElseThrow(() -> new ResourceNotFoundException("Recruiter", dto.recruiterId())));

        drive.setCourse(dto.courseId() == null ? null
                : courseRepository.findById(dto.courseId())
                        .orElseThrow(() -> new ResourceNotFoundException("Course", dto.courseId())));

        drive.setDriveDate(dto.driveDate());
        drive.setPosition(dto.position().trim());
        drive.setDescription(dto.description());
        drive.setEligibilityCriteria(dto.eligibilityCriteria());
        drive.setPackageAmount(dto.packageAmount());
        drive.setHrContactName(dto.hrContactName());
        drive.setHrContactEmail(dto.hrContactEmail());
        drive.setHrContactPhone(dto.hrContactPhone());
        drive.setNoOfOpenings(dto.noOfOpenings());
        drive.setNoOfStudentsSelected(dto.noOfStudentsSelected());

        if (dto.noOfOpenings() != null && dto.noOfStudentsSelected() != null
                && dto.noOfStudentsSelected() > dto.noOfOpenings()) {
            throw new BusinessRuleException("More students selected than there were openings.");
        }

        if (dto.driveMode() != null && !dto.driveMode().isBlank()) {
            drive.setDriveMode(parseMode(dto.driveMode()));
        }
        if (dto.driveStatus() != null && !dto.driveStatus().isBlank()) {
            drive.setDriveStatus(parseStatus(dto.driveStatus()));
        }
    }

    private PlacementDrive.Mode parseMode(String raw) {
        for (PlacementDrive.Mode m : PlacementDrive.Mode.values()) {
            if (m.name().equalsIgnoreCase(raw)) return m;
        }
        throw new BusinessRuleException("Unknown drive mode: '" + raw + "'");
    }

    private PlacementDrive.Status parseStatus(String raw) {
        for (PlacementDrive.Status s : PlacementDrive.Status.values()) {
            if (s.name().equalsIgnoreCase(raw)) return s;
        }
        throw new BusinessRuleException("Unknown drive status: '" + raw + "'");
    }

    private PlacementDriveDto toDto(PlacementDrive d) {
        return new PlacementDriveDto(
                d.getDriveId(),
                d.getRecruiter().getRecruiterId(), d.getRecruiter().getCompanyName(),
                d.getCourse() != null ? d.getCourse().getCourseId() : null,
                d.getCourse() != null ? d.getCourse().getName() : null,
                d.getDriveDate(),
                d.getDriveMode().name(),
                d.getPosition(), d.getDescription(), d.getEligibilityCriteria(),
                d.getPackageAmount(), d.getHrContactName(), d.getHrContactEmail(), d.getHrContactPhone(),
                d.getNoOfOpenings(), d.getNoOfStudentsSelected(),
                d.getDriveStatus().name());
    }
}
