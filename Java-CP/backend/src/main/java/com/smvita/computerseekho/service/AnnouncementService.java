package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.AnnouncementDto;
import com.smvita.computerseekho.entity.Announcement;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.AnnouncementRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDate;
import java.util.List;

@Service
@RequiredArgsConstructor
@Transactional
public class AnnouncementService {

    private final AnnouncementRepository announcementRepository;

    public List<AnnouncementDto> findAll() {
        return announcementRepository.findAll().stream().map(this::toDto).toList();
    }

    // Home page ticker consumes this.
    public List<AnnouncementDto> findCurrentlyValid() {
        return announcementRepository.findCurrentlyValid(LocalDate.now()).stream().map(this::toDto).toList();
    }

    public AnnouncementDto findById(Integer id) {
        return toDto(getEntityOrThrow(id));
    }

    public AnnouncementDto create(AnnouncementDto dto) {
        Announcement announcement = new Announcement();
        applyDto(announcement, dto);
        return toDto(announcementRepository.save(announcement));
    }

    public AnnouncementDto update(Integer id, AnnouncementDto dto) {
        Announcement announcement = getEntityOrThrow(id);
        applyDto(announcement, dto);
        return toDto(announcementRepository.save(announcement));
    }

    public void delete(Integer id) {
        announcementRepository.delete(getEntityOrThrow(id));
    }

    private Announcement getEntityOrThrow(Integer id) {
        return announcementRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Announcement", id));
    }

    private void applyDto(Announcement a, AnnouncementDto dto) {
        a.setContent(dto.content());
        a.setStartDate(dto.startDate());
        a.setEndDate(dto.endDate());
        a.setDisplayOrder(dto.displayOrder() != null ? dto.displayOrder() : 0);
        a.setIsActive(dto.isActive() != null ? dto.isActive() : true);
    }

    private AnnouncementDto toDto(Announcement a) {
        return new AnnouncementDto(a.getAnnouncementId(), a.getContent(), a.getStartDate(),
                a.getEndDate(), a.getDisplayOrder(), a.getIsActive());
    }
}
