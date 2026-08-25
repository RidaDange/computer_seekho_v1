package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.NewsEvent;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface NewsEventRepository extends JpaRepository<NewsEvent, Integer> {
    List<NewsEvent> findByIsActiveTrue();
}
