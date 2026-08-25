package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.TestimonialDto;
import com.smvita.computerseekho.entity.Testimonial;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.TestimonialRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Service
@RequiredArgsConstructor
@Transactional
public class TestimonialService {

    private final TestimonialRepository testimonialRepository;

    public List<TestimonialDto> findAll() {
        return testimonialRepository.findAll().stream().map(this::toDto).toList();
    }

    public List<TestimonialDto> findApproved() {
        return testimonialRepository.findByIsApprovedTrue().stream().map(this::toDto).toList();
    }

    public TestimonialDto findById(Integer id) {
        return toDto(getEntityOrThrow(id));
    }

    public TestimonialDto create(TestimonialDto dto) {
        Testimonial testimonial = new Testimonial();
        applyDto(testimonial, dto);
        return toDto(testimonialRepository.save(testimonial));
    }

    public TestimonialDto update(Integer id, TestimonialDto dto) {
        Testimonial testimonial = getEntityOrThrow(id);
        applyDto(testimonial, dto);
        return toDto(testimonialRepository.save(testimonial));
    }

    public void delete(Integer id) {
        testimonialRepository.delete(getEntityOrThrow(id));
    }

    private Testimonial getEntityOrThrow(Integer id) {
        return testimonialRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Testimonial", id));
    }

    private void applyDto(Testimonial t, TestimonialDto dto) {
        t.setName(dto.name());
        t.setContent(dto.content());
        t.setRating(dto.rating() != null ? dto.rating().byteValue() : null);
        t.setPhotoUrl(dto.photoUrl());
        t.setIsApproved(dto.isApproved() != null ? dto.isApproved() : false);
    }

    private TestimonialDto toDto(Testimonial t) {
        return new TestimonialDto(t.getTestimonialId(), t.getName(), t.getContent(),
                t.getRating() != null ? t.getRating().intValue() : null, t.getPhotoUrl(), t.getIsApproved());
    }
}
