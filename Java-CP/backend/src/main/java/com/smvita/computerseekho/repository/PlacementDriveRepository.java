package com.smvita.computerseekho.repository;

import com.smvita.computerseekho.entity.PlacementDrive;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface PlacementDriveRepository extends JpaRepository<PlacementDrive, Integer> {
    List<PlacementDrive> findAllByOrderByDriveDateDescDriveIdDesc();

    List<PlacementDrive> findByRecruiter_RecruiterIdOrderByDriveDateDesc(Integer recruiterId);
}
