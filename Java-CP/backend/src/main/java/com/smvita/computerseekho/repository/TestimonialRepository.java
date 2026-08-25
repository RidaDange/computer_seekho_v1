package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.Testimonial;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface TestimonialRepository extends JpaRepository<Testimonial, Integer> {
    // Public site only shows approved testimonials.
    List<Testimonial> findByIsApprovedTrue();
}
