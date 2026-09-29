-- Read-only: compare pricing configuration between qalam_staging and qalam_prod.
-- Domains are matched by Code (ids differ between environments).
-- Only the base market (sa) is compared for domain rates; other markets are derived from it via exchange rates.
SET NOCOUNT ON;

PRINT '1) Base (sa) domain rates that differ';
;WITH stg AS (
    SELECT d.Code, p.SessionTypeCode, p.PricePerHour,
           ROW_NUMBER() OVER (PARTITION BY d.Code, p.SessionTypeCode ORDER BY p.EffectiveFrom DESC, p.Id DESC) AS rn
    FROM qalam_staging.pricing.DomainSessionPrices p
    JOIN qalam_staging.education.EducationDomains d ON d.Id = p.DomainId
    WHERE p.MarketCode = 'sa' AND p.IsActive = 1 AND p.EffectiveTo IS NULL
),
prd AS (
    SELECT d.Code, p.SessionTypeCode, p.PricePerHour,
           ROW_NUMBER() OVER (PARTITION BY d.Code, p.SessionTypeCode ORDER BY p.EffectiveFrom DESC, p.Id DESC) AS rn
    FROM qalam_prod.pricing.DomainSessionPrices p
    JOIN qalam_prod.education.EducationDomains d ON d.Id = p.DomainId
    WHERE p.MarketCode = 'sa' AND p.IsActive = 1 AND p.EffectiveTo IS NULL
)
SELECT COALESCE(s.Code, p.Code) AS DomainCode,
       COALESCE(s.SessionTypeCode, p.SessionTypeCode) AS SessionType,
       s.PricePerHour AS StagingPrice,
       p.PricePerHour AS ProdPrice
FROM (SELECT * FROM stg WHERE rn = 1) s
FULL OUTER JOIN (SELECT * FROM prd WHERE rn = 1) p
    ON p.Code = s.Code AND p.SessionTypeCode = s.SessionTypeCode
WHERE s.PricePerHour IS NULL OR p.PricePerHour IS NULL OR s.PricePerHour <> p.PricePerHour
ORDER BY DomainCode, SessionType;

PRINT '2) Markets / exchange rates that differ';
SELECT COALESCE(s.Code, p.Code) AS MarketCode,
       s.ExchangeRateFromBase AS StagingRate, p.ExchangeRateFromBase AS ProdRate,
       s.IsActive AS StagingActive, p.IsActive AS ProdActive,
       s.IsDefault AS StagingDefault, p.IsDefault AS ProdDefault
FROM qalam_staging.pricing.PricingMarkets s
FULL OUTER JOIN qalam_prod.pricing.PricingMarkets p ON p.Code = s.Code
WHERE s.Code IS NULL OR p.Code IS NULL
   OR s.ExchangeRateFromBase <> p.ExchangeRateFromBase
   OR s.IsActive <> p.IsActive OR s.IsDefault <> p.IsDefault
ORDER BY MarketCode;

PRINT '3) Teacher level tiers that differ';
SELECT COALESCE(s.Code, p.Code) AS LevelCode,
       s.TeacherSharePct AS StagingSharePct, p.TeacherSharePct AS ProdSharePct,
       s.OrderIndex AS StagingOrder, p.OrderIndex AS ProdOrder,
       s.IsActive AS StagingActive, p.IsActive AS ProdActive
FROM qalam_staging.teacher.TeacherLevels s
FULL OUTER JOIN qalam_prod.teacher.TeacherLevels p ON p.Code = s.Code
WHERE s.Code IS NULL OR p.Code IS NULL
   OR s.TeacherSharePct <> p.TeacherSharePct
   OR s.OrderIndex <> p.OrderIndex OR s.IsActive <> p.IsActive
ORDER BY LevelCode;
