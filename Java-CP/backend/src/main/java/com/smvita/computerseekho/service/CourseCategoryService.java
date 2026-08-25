package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.CourseCategoryDto;
import com.smvita.computerseekho.entity.CourseCategory;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.CourseCategoryRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Service
@RequiredArgsConstructor
@Transactional
public class CourseCategoryService {

    private final CourseCategoryRepository courseCategoryRepository;

    public List<CourseCategoryDto> findAll() {
        return courseCategoryRepository.findAll().stream().map(this::toDto).toList();
    }

    public List<CourseCategoryDto> findActive() {
        return courseCategoryRepository.findByIsActiveTrue().stream().map(this::toDto).toList();
    }

    public CourseCategoryDto findById(Integer id) {
        return toDto(getEntityOrThrow(id));
    }

    public CourseCategoryDto create(CourseCategoryDto dto) {
        CourseCategory category = new CourseCategory();
        applyDto(category, dto);
        return toDto(courseCategoryRepository.save(category));
    }

    public CourseCategoryDto update(Integer id, CourseCategoryDto dto) {
        CourseCategory category = getEntityOrThrow(id);
        applyDto(category, dto);
        return toDto(courseCategoryRepository.save(category));
    }

    public void delete(Integer id) {
        courseCategoryRepository.delete(getEntityOrThrow(id));
    }

    private CourseCategory getEntityOrThrow(Integer id) {
        return courseCategoryRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Course Category", id));
    }

    private void applyDto(CourseCategory category, CourseCategoryDto dto) {
        category.setName(dto.name());
        category.setAgeGroup(dto.ageGroup());
        category.setDescription(dto.description());
        category.setIsActive(dto.isActive() != null ? dto.isActive() : true);
    }

    private CourseCategoryDto toDto(CourseCategory c) {
        return new CourseCategoryDto(c.getCategoryId(), c.getName(), c.getAgeGroup(), c.getDescription(), c.getIsActive());
    }
}
