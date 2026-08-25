package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.Student;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface StudentRepository extends JpaRepository<Student, Integer> {

    /**
     * "No enquiry, no registration" runs one way at the DB level
     * (students.inquiry_id NOT NULL) but the reverse also has to hold: one
     * enquiry produces at most one student. Nothing in the schema stops a
     * second registration against the same enquiry, so the service checks
     * this before inserting.
     */
    Optional<Student> findByInquiry_InquiryId(Integer inquiryId);

    boolean existsByInquiry_InquiryId(Integer inquiryId);

    /** students.email is UNIQUE — checked up front for a readable error. */
    boolean existsByEmailIgnoreCase(String email);

    List<Student> findAllByOrderByStudentIdDesc();
}
