package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.BannerDto;
import com.smvita.computerseekho.entity.Banner;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.BannerRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDate;
import java.util.List;

@Service
@RequiredArgsConstructor
@Transactional
public class BannerService {

    private final BannerRepository bannerRepository;

    public List<BannerDto> findAll() {
        return bannerRepository.findAll().stream().map(this::toDto).toList();
    }

    public List<BannerDto> findCurrentlyValid() {
        return bannerRepository.findCurrentlyValid(LocalDate.now()).stream().map(this::toDto).toList();
    }

    public BannerDto findById(Integer id) {
        return toDto(getEntityOrThrow(id));
    }

    public BannerDto create(BannerDto dto) {
        Banner banner = new Banner();
        applyDto(banner, dto);
        return toDto(bannerRepository.save(banner));
    }

    public BannerDto update(Integer id, BannerDto dto) {
        Banner banner = getEntityOrThrow(id);
        applyDto(banner, dto);
        return toDto(bannerRepository.save(banner));
    }

    public void delete(Integer id) {
        bannerRepository.delete(getEntityOrThrow(id));
    }

    private Banner getEntityOrThrow(Integer id) {
        return bannerRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Banner", id));
    }

    private void applyDto(Banner b, BannerDto dto) {
        b.setTitle(dto.title());
        b.setImageUrl(dto.imageUrl());
        b.setLinkUrl(dto.linkUrl());
        b.setDisplayOrder(dto.displayOrder() != null ? dto.displayOrder() : 0);
        b.setIsActive(dto.isActive() != null ? dto.isActive() : true);
        b.setStartDate(dto.startDate());
        b.setEndDate(dto.endDate());
    }

    private BannerDto toDto(Banner b) {
        return new BannerDto(b.getBannerId(), b.getTitle(), b.getImageUrl(), b.getLinkUrl(),
                b.getDisplayOrder(), b.getIsActive(), b.getStartDate(), b.getEndDate());
    }
}
