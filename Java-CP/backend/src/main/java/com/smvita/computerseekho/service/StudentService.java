package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.StudentDto;
import com.smvita.computerseekho.entity.Enrollment;
import com.smvita.computerseekho.entity.Student;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.EnrollmentRepository;
import com.smvita.computerseekho.repository.StudentRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Service
@RequiredArgsConstructor
@Transactional(readOnly = true)
public class StudentService {

    private final StudentRepository studentRepository;
    private final EnrollmentRepository enrollmentRepository;

    public List<StudentDto> findAll() {
        return studentRepository.findAllByOrderByStudentIdDesc().stream().map(this::toDto).toList();
    }

    public StudentDto findById(Integer id) {
        return toDto(studentRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Student", id)));
    }

    /** The batch roster — enrolled students in one batch. */
    public List<StudentDto> findByBatch(Integer batchId) {
        return enrollmentRepository.findByBatch_BatchIdOrderByEnrollDateAscEnrollmentIdAsc(batchId)
                .stream()
                .map(e -> toDto(e.getStudent(), e))
                .toList();
    }

    private StudentDto toDto(Student s) {
        // Most recent enrolment wins: the BRD's Course-A-then-Course-B case
        // means a student can hold several, and the list should show where
        // they are now rather than where they started.
        Enrollment latest = enrollmentRepository
                .findByStudent_StudentIdOrderByEnrollmentIdDesc(s.getStudentId())
                .stream().findFirst().orElse(null);
        return toDto(s, latest);
    }

    StudentDto toDto(Student s, Enrollment e) {
        return new StudentDto(
                s.getStudentId(),
                s.getInquiry() != null ? s.getInquiry().getInquiryId() : null,
                s.getFirstName(), s.getLastName(), s.getParentName(), s.getParentPhone(),
                s.getEmail(), s.getPhone(), s.getDob(),
                s.getGender() != null ? s.getGender().name() : null,
                s.getAddressLine1(), s.getAddressLine2(), s.getCity(), s.getState(), s.getPincode(),
                s.getPhotoUrl(), s.getQualification(), s.getRegDate(),
                e != null ? e.getEnrollmentId() : null,
                e != null ? e.getBatch().getBatchId() : null,
                e != null ? e.getBatch().getBatchName() : null,
                e != null ? e.getBatch().getCourse().getName() : null);
    }
}
