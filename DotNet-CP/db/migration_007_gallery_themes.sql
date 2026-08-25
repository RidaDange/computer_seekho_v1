-- ============================================================
--  Campus Life themes
--
--  gallery_images.category already existed but held one-off values
--  (Academics / Events / Batches / Placement Prep) with exactly one photo
--  each, so the page rendered four single cards with nothing behind them.
--
--  This realigns the categories to the themes the page now navigates by,
--  and gives each one several photos so clicking through actually shows a
--  collection. The theme names double as the Campus Life tile labels and
--  as the URL segment (/campus-life/gallery/Lab%20Sessions), so keep them
--  short and human-readable when adding more.
--
--  Safe to re-run.
-- ============================================================

USE computerseekho;

DELETE FROM gallery_images;
ALTER TABLE gallery_images AUTO_INCREMENT = 1;

INSERT INTO gallery_images (title, description, image_url, category, upload_date, is_active) VALUES
  -- Lab Sessions
  ('Hands-on Lab',        'Students working through a guided lab exercise',        'https://placehold.co/800x600?text=Hands-on+Lab',        'Lab Sessions',    '2026-02-10', 1),
  ('Cloud Lab',           'Deploying a containerised app in the cloud lab',        'https://placehold.co/800x600?text=Cloud+Lab',           'Lab Sessions',    '2026-03-04', 1),
  ('Database Workshop',   'Query tuning workshop in the DB lab',                   'https://placehold.co/800x600?text=Database+Workshop',   'Lab Sessions',    '2026-03-18', 1),
  ('Late-night Debugging','Project week in the open lab',                          'https://placehold.co/800x600?text=Project+Week',        'Lab Sessions',    '2026-04-02', 1),

  -- Guest Lectures
  ('Cloud Architecture',  'Industry expert on designing for scale',                'https://placehold.co/800x600?text=Cloud+Architecture',  'Guest Lectures',  '2026-01-22', 1),
  ('Life After PG-DAC',   'Alumni panel on the first year in industry',            'https://placehold.co/800x600?text=Alumni+Panel',        'Guest Lectures',  '2026-02-19', 1),
  ('AI in Practice',      'Guest session on production machine learning',          'https://placehold.co/800x600?text=AI+in+Practice',      'Guest Lectures',  '2026-04-15', 1),

  -- Batch Photos
  ('PG-DAC Aug 2024',     'The PG-DAC August 2024 cohort',                         'https://placehold.co/800x600?text=PG-DAC+Aug+2024',     'Batch Photos',    '2024-08-05', 1),
  ('PG-DBDA Aug 2024',    'The PG-DBDA August 2024 cohort',                        'https://placehold.co/800x600?text=PG-DBDA+Aug+2024',    'Batch Photos',    '2024-08-05', 1),
  ('Induction Day',       'First day of the new academic year',                    'https://placehold.co/800x600?text=Induction+Day',       'Batch Photos',    '2026-08-03', 1),

  -- Mock Interviews
  ('Mock Interview Day',  'Students in mock technical interviews',                 'https://placehold.co/800x600?text=Mock+Interviews',     'Mock Interviews', '2026-01-15', 1),
  ('Group Discussion',    'GD round practice ahead of placement season',           'https://placehold.co/800x600?text=Group+Discussion',    'Mock Interviews', '2026-01-16', 1),
  ('Resume Clinic',       'One-to-one resume reviews with the placement cell',     'https://placehold.co/800x600?text=Resume+Clinic',       'Mock Interviews', '2026-01-20', 1);

SELECT category, COUNT(*) AS photos FROM gallery_images WHERE is_active = 1 GROUP BY category ORDER BY category;
