-- ============================================================
--  Sample batch albums, so the public Campus Life page has something
--  to show. Albums with no photos are filtered out of the public
--  listing by design, so an album row on its own wouldn't appear.
--
--  Safe to re-run — clears the rows it owns first. cover_image_id is
--  nulled before deleting the images because of fk_album_cover_image.
-- ============================================================

USE computerseekho;

UPDATE batch_albums SET cover_image_id = NULL;
DELETE FROM batch_album_images;
DELETE FROM batch_albums;

INSERT INTO batch_albums (album_id, batch_id, title, description, cover_image_id, is_active) VALUES
  (1, 1, 'PG-DAC Aug 2024 — Batch Album',
   'Classroom sessions, project demos and the farewell for the PG-DAC August 2024 cohort.', NULL, 1),
  (2, 2, 'PG-DBDA Aug 2024 — Batch Album',
   'Analytics labs and the capstone presentation day.', NULL, 1);

INSERT INTO batch_album_images (image_id, album_id, image_url, caption, uploaded_by, upload_date, display_order, is_active) VALUES
  (1, 1, 'https://placehold.co/800x600?text=PG-DAC+Group+Photo',   'The full batch on induction day',        3, '2024-08-05', 1, 1),
  (2, 1, 'https://placehold.co/800x600?text=Project+Demo',          'Final project demonstrations',          3, '2025-01-20', 2, 1),
  (3, 1, 'https://placehold.co/800x600?text=Lab+Session',           'Cloud computing lab',                   3, '2024-10-12', 3, 1),
  (4, 1, 'https://placehold.co/800x600?text=Farewell',              'Farewell, January 2025',                3, '2025-01-30', 4, 1),
  (5, 2, 'https://placehold.co/800x600?text=PG-DBDA+Group+Photo',   'PG-DBDA August 2024 batch',             4, '2024-08-05', 1, 1),
  (6, 2, 'https://placehold.co/800x600?text=Capstone+Day',          'Capstone presentation day',             4, '2025-01-22', 2, 1);

-- Designate the covers. Set after the images exist, since cover_image_id
-- is an FK onto batch_album_images.
UPDATE batch_albums SET cover_image_id = 1 WHERE album_id = 1;
UPDATE batch_albums SET cover_image_id = 5 WHERE album_id = 2;

SELECT a.album_id, b.batch_name, a.title, a.cover_image_id,
       (SELECT COUNT(*) FROM batch_album_images i WHERE i.album_id = a.album_id AND i.is_active = 1) AS photos
FROM batch_albums a JOIN batches b ON b.batch_id = a.batch_id
ORDER BY a.album_id;
