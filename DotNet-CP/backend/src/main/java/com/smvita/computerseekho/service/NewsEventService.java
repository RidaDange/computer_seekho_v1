package com.smvita.computerseekho.service;

import com.smvita.computerseekho.dto.NewsEventDto;
import com.smvita.computerseekho.entity.NewsEvent;
import com.smvita.computerseekho.exception.ResourceNotFoundException;
import com.smvita.computerseekho.repository.NewsEventRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Service
@RequiredArgsConstructor
@Transactional
public class NewsEventService {

    private final NewsEventRepository newsEventRepository;

    public List<NewsEventDto> findAll() {
        return newsEventRepository.findAll().stream().map(this::toDto).toList();
    }

    public List<NewsEventDto> findActive() {
        return newsEventRepository.findByIsActiveTrue().stream().map(this::toDto).toList();
    }

    public NewsEventDto findById(Integer id) {
        return toDto(getEntityOrThrow(id));
    }

    public NewsEventDto create(NewsEventDto dto) {
        NewsEvent newsEvent = new NewsEvent();
        applyDto(newsEvent, dto);
        return toDto(newsEventRepository.save(newsEvent));
    }

    public NewsEventDto update(Integer id, NewsEventDto dto) {
        NewsEvent newsEvent = getEntityOrThrow(id);
        applyDto(newsEvent, dto);
        return toDto(newsEventRepository.save(newsEvent));
    }

    public void delete(Integer id) {
        newsEventRepository.delete(getEntityOrThrow(id));
    }

    private NewsEvent getEntityOrThrow(Integer id) {
        return newsEventRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("News/Event", id));
    }

    private void applyDto(NewsEvent n, NewsEventDto dto) {
        n.setTitle(dto.title());
        n.setContent(dto.content());
        n.setImageUrl(dto.imageUrl());
        n.setEventDate(dto.eventDate());
        n.setIsActive(dto.isActive() != null ? dto.isActive() : true);
    }

    private NewsEventDto toDto(NewsEvent n) {
        return new NewsEventDto(n.getNewsId(), n.getTitle(), n.getContent(),
                n.getImageUrl(), n.getEventDate(), n.getIsActive());
    }
}
