-- Classify education domains (run on staging/prod before/after API restart with remediation seeder).
SELECT Id, Code, NameAr, NameEn, IsActive, CreatedAt
FROM education.EducationDomains
ORDER BY NameAr, CreatedAt;

SELECT d.Id, d.Code, d.IsActive, d.CreatedAt,
       COUNT(q.Id) AS QuestionCount,
       SUM(CASE WHEN q.IsSystem = 1 THEN 1 ELSE 0 END) AS SystemQs,
       SUM(CASE WHEN q.IsSystem = 0 THEN 1 ELSE 0 END) AS CustomQs
FROM education.EducationDomains d
LEFT JOIN teacher.TeacherDomainQuestions q ON q.DomainId = d.Id
GROUP BY d.Id, d.Code, d.IsActive, d.CreatedAt
ORDER BY d.Code, d.CreatedAt;

-- Active duplicates by Arabic name
SELECT NameAr, COUNT(*) AS Cnt
FROM education.EducationDomains
WHERE IsActive = 1
GROUP BY NameAr
HAVING COUNT(*) > 1;
