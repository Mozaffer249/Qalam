-- Post-merge verification for education domain remediation (run on prod after API seed/restart).
-- Expected active codes (exactly once each):
--   school, quran, language, university, soft-skills, life-skills, tech-skills,
--   hobbies, finance, knowledge, sharia
-- Expected inactive: soft-skills-archive-*, …, try_1, try_88, and any *-archive-* donors.

-- 1) Active domains by code
SELECT Id, Code, NameAr, NameEn, IsActive, CreatedAt
FROM education.EducationDomains
WHERE IsActive = 1
ORDER BY Code;

-- 2) No duplicate active codes
SELECT Code, COUNT(*) AS Cnt
FROM education.EducationDomains
WHERE IsActive = 1
GROUP BY Code
HAVING COUNT(*) > 1;

-- 3) No duplicate active Arabic names
SELECT NameAr, COUNT(*) AS Cnt
FROM education.EducationDomains
WHERE IsActive = 1
GROUP BY NameAr
HAVING COUNT(*) > 1;

-- 4) try_* must not be active
SELECT Id, Code, NameAr, IsActive
FROM education.EducationDomains
WHERE Code LIKE N'try[_]%' AND IsActive = 1;

-- 5) Canonical codes present and active (expect 11 rows)
SELECT Code
FROM education.EducationDomains
WHERE IsActive = 1
  AND Code IN (
    N'school', N'quran', N'language', N'university',
    N'soft-skills', N'life-skills', N'tech-skills',
    N'hobbies', N'finance', N'knowledge', N'sharia'
  )
ORDER BY Code;

-- 6) Archived seed donors (expect inactive)
SELECT Id, Code, NameAr, IsActive
FROM education.EducationDomains
WHERE Code LIKE N'%-archive-%'
ORDER BY Id;

-- 7) Custom vs system questions on active keepers
SELECT d.Id, d.Code, d.NameAr,
       SUM(CASE WHEN q.IsSystem = 0 AND q.IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCustomQs,
       SUM(CASE WHEN q.IsSystem = 1 AND q.IsActive = 1 THEN 1 ELSE 0 END) AS ActiveSystemQs
FROM education.EducationDomains d
LEFT JOIN teacher.TeacherDomainQuestions q ON q.DomainId = d.Id
WHERE d.IsActive = 1
GROUP BY d.Id, d.Code, d.NameAr
ORDER BY d.Code;

-- 8) knowledge / university / soft-skills should keep approved DomainIds with custom Qs
--    (prod txfiles: knowledge≈4, university≈12, soft-skills≈10 — ids may differ if remapped elsewhere)
SELECT d.Id, d.Code,
       SUM(CASE WHEN q.IsSystem = 0 AND q.IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCustomQs
FROM education.EducationDomains d
LEFT JOIN teacher.TeacherDomainQuestions q ON q.DomainId = d.Id
WHERE d.IsActive = 1
  AND d.Code IN (N'knowledge', N'university', N'soft-skills')
GROUP BY d.Id, d.Code;

-- 9) life-skills / sharia: stacked UI requires rule flags + Excel catalog rows
SELECT d.Id, d.Code,
       r.HasParentSubject,
       r.EducationLevelAfterSubject,
       SUM(CASE WHEN s.ParentSubjectId IS NULL THEN 1 ELSE 0 END) AS RootSubjects,
       SUM(CASE WHEN s.Code LIKE N'life.%' OR s.Code LIKE N'sharia.%' THEN 1 ELSE 0 END) AS ExcelCatalogRows
FROM education.EducationDomains d
JOIN teaching.EducationRules r ON r.DomainId = d.Id
LEFT JOIN education.Subjects s ON s.DomainId = d.Id AND s.IsActive = 1
WHERE d.IsActive = 1
  AND d.Code IN (N'life-skills', N'sharia')
GROUP BY d.Id, d.Code, r.HasParentSubject, r.EducationLevelAfterSubject;
-- Expected: HasParentSubject=1, EducationLevelAfterSubject=1, ExcelCatalogRows > 0
