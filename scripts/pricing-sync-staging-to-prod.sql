-- Sync pricing configuration FROM qalam_staging TO qalam_prod.
--   * teacher.TeacherLevels      matched by Code (update / insert; prod-only levels are deactivated, never deleted)
--   * pricing.PricingMarkets     matched by Code (update / insert; prod-only markets are deactivated)
--   * pricing.DomainSessionPrices base market (sa) matched by domain Code, then every active
--     non-base market is re-derived: ROUND(sa price * ExchangeRateFromBase, 2) — same as the API.
-- Rate changes keep history: the open row is closed (EffectiveTo = now) and a new row is inserted.
-- Domains that exist only in prod keep their prod price.
--
-- @DryRun = 1 (default): everything runs inside a transaction and is ROLLED BACK; you only see the preview.
-- @DryRun = 0          : same, but COMMITTED.
-- Run the whole file with F5 (nothing selected).

USE qalam_prod;
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'qalam_prod'
BEGIN
    RAISERROR('Run this script against qalam_prod only.', 16, 1);
    RETURN;
END

DECLARE @DryRun bit = 1;
DECLARE @Now datetime2 = SYSUTCDATETIME();
DECLARE @Base nvarchar(10) = N'sa';
DECLARE @Counts TABLE (Step nvarchar(40) NOT NULL);

DROP TABLE IF EXISTS #StgLevels, #StgMarkets, #StgBase, #Target, #Changed;

IF EXISTS (SELECT Code FROM qalam_prod.education.EducationDomains GROUP BY Code HAVING COUNT(*) > 1)
BEGIN
    RAISERROR('qalam_prod has duplicate EducationDomains.Code values; fix domains first.', 16, 1);
    RETURN;
END

---------------------------------------------------------------------------
-- Staging snapshot
---------------------------------------------------------------------------
CREATE TABLE #StgLevels (
    Code nvarchar(30) COLLATE DATABASE_DEFAULT PRIMARY KEY,
    NameAr nvarchar(50) COLLATE DATABASE_DEFAULT, NameEn nvarchar(50) COLLATE DATABASE_DEFAULT,
    OrderIndex int, TeacherSharePct decimal(5,2), IsActive bit);
INSERT #StgLevels
SELECT Code, NameAr, NameEn, OrderIndex, TeacherSharePct, IsActive
FROM qalam_staging.teacher.TeacherLevels;

CREATE TABLE #StgMarkets (
    Code nvarchar(10) COLLATE DATABASE_DEFAULT PRIMARY KEY,
    NameEn nvarchar(100) COLLATE DATABASE_DEFAULT, NameAr nvarchar(100) COLLATE DATABASE_DEFAULT,
    Currency nvarchar(3) COLLATE DATABASE_DEFAULT, IsActive bit, IsDefault bit, ExchangeRateFromBase decimal(18,6));
INSERT #StgMarkets
SELECT Code, NameEn, NameAr, Currency, IsActive, IsDefault, ExchangeRateFromBase
FROM qalam_staging.pricing.PricingMarkets;

CREATE TABLE #StgBase (
    DomainCode nvarchar(100) COLLATE DATABASE_DEFAULT, SessionTypeCode nvarchar(30) COLLATE DATABASE_DEFAULT,
    PricePerHour decimal(18,2), PRIMARY KEY (DomainCode, SessionTypeCode));
INSERT #StgBase
SELECT Code, SessionTypeCode, PricePerHour
FROM (
    SELECT d.Code, p.SessionTypeCode, p.PricePerHour,
           ROW_NUMBER() OVER (PARTITION BY d.Code, p.SessionTypeCode ORDER BY p.EffectiveFrom DESC, p.Id DESC) AS rn
    FROM qalam_staging.pricing.DomainSessionPrices p
    JOIN qalam_staging.education.EducationDomains d ON d.Id = p.DomainId
    WHERE p.MarketCode = @Base AND p.IsActive = 1 AND p.EffectiveTo IS NULL
) x
WHERE rn = 1;

CREATE TABLE #Target (
    DomainId int, MarketCode nvarchar(10) COLLATE DATABASE_DEFAULT,
    SessionTypeCode nvarchar(30) COLLATE DATABASE_DEFAULT, PricePerHour decimal(18,2),
    PRIMARY KEY (DomainId, MarketCode, SessionTypeCode));
CREATE TABLE #Changed (
    DomainId int, MarketCode nvarchar(10) COLLATE DATABASE_DEFAULT,
    SessionTypeCode nvarchar(30) COLLATE DATABASE_DEFAULT, OldPrice decimal(18,2) NULL, NewPrice decimal(18,2));

---------------------------------------------------------------------------
-- Preview (before changes)
---------------------------------------------------------------------------
PRINT '--- Teacher levels: staging vs prod ---';
SELECT COALESCE(s.Code, p.Code) AS LevelCode,
       s.TeacherSharePct AS StagingSharePct, p.TeacherSharePct AS ProdSharePct,
       s.OrderIndex AS StagingOrder, p.OrderIndex AS ProdOrder,
       s.IsActive AS StagingActive, p.IsActive AS ProdActive,
       (SELECT COUNT(*) FROM qalam_prod.dbo.Teachers t WHERE t.TeacherLevelId = p.Id) AS ProdTeachersOnLevel,
       CASE WHEN p.Code IS NULL THEN 'INSERT'
            WHEN s.Code IS NULL THEN 'DEACTIVATE'
            ELSE 'UPDATE' END AS Action
FROM #StgLevels s
FULL OUTER JOIN qalam_prod.teacher.TeacherLevels p ON p.Code = s.Code
WHERE s.Code IS NULL OR p.Code IS NULL
   OR s.TeacherSharePct <> p.TeacherSharePct OR s.OrderIndex <> p.OrderIndex
   OR s.IsActive <> p.IsActive OR s.NameAr <> p.NameAr OR s.NameEn <> p.NameEn
ORDER BY LevelCode;

PRINT '--- Markets: staging vs prod ---';
SELECT COALESCE(s.Code, p.Code) AS MarketCode,
       s.ExchangeRateFromBase AS StagingRate, p.ExchangeRateFromBase AS ProdRate,
       s.IsActive AS StagingActive, p.IsActive AS ProdActive,
       s.IsDefault AS StagingDefault, p.IsDefault AS ProdDefault,
       CASE WHEN p.Code IS NULL THEN 'INSERT'
            WHEN s.Code IS NULL THEN 'DEACTIVATE'
            ELSE 'UPDATE' END AS Action
FROM #StgMarkets s
FULL OUTER JOIN qalam_prod.pricing.PricingMarkets p ON p.Code = s.Code
WHERE s.Code IS NULL OR p.Code IS NULL
   OR s.ExchangeRateFromBase <> p.ExchangeRateFromBase OR s.IsActive <> p.IsActive
   OR s.IsDefault <> p.IsDefault OR s.Currency <> p.Currency
   OR s.NameEn <> p.NameEn OR s.NameAr <> p.NameAr
ORDER BY MarketCode;

PRINT '--- Staging domain codes with no matching prod domain (skipped) ---';
SELECT DISTINCT s.DomainCode
FROM #StgBase s
WHERE NOT EXISTS (SELECT 1 FROM qalam_prod.education.EducationDomains d WHERE d.Code = s.DomainCode);

---------------------------------------------------------------------------
-- Apply
---------------------------------------------------------------------------
BEGIN TRY
    BEGIN TRANSACTION;

    -- 0) Close duplicate open rate rows (keep newest per market/domain/session type)
    ;WITH d AS (
        SELECT Id, ROW_NUMBER() OVER (PARTITION BY MarketCode, DomainId, SessionTypeCode
                                      ORDER BY EffectiveFrom DESC, Id DESC) AS rn
        FROM qalam_prod.pricing.DomainSessionPrices
        WHERE EffectiveTo IS NULL AND IsActive = 1
    )
    UPDATE p SET EffectiveTo = @Now, UpdatedAt = @Now
    OUTPUT N'DuplicateOpenRatesClosed' INTO @Counts(Step)
    FROM qalam_prod.pricing.DomainSessionPrices p JOIN d ON d.Id = p.Id
    WHERE d.rn > 1;

    -- 1) Teacher levels
    UPDATE p SET p.NameAr = s.NameAr, p.NameEn = s.NameEn, p.OrderIndex = s.OrderIndex,
                 p.TeacherSharePct = s.TeacherSharePct, p.IsActive = s.IsActive, p.UpdatedAt = @Now
    OUTPUT N'LevelsUpdated' INTO @Counts(Step)
    FROM qalam_prod.teacher.TeacherLevels p JOIN #StgLevels s ON s.Code = p.Code
    WHERE s.TeacherSharePct <> p.TeacherSharePct OR s.OrderIndex <> p.OrderIndex
       OR s.IsActive <> p.IsActive OR s.NameAr <> p.NameAr OR s.NameEn <> p.NameEn;

    INSERT qalam_prod.teacher.TeacherLevels (Code, NameAr, NameEn, OrderIndex, TeacherSharePct, IsActive, CreatedAt)
    OUTPUT N'LevelsInserted' INTO @Counts(Step)
    SELECT s.Code, s.NameAr, s.NameEn, s.OrderIndex, s.TeacherSharePct, s.IsActive, @Now
    FROM #StgLevels s
    WHERE NOT EXISTS (SELECT 1 FROM qalam_prod.teacher.TeacherLevels p WHERE p.Code = s.Code);

    UPDATE p SET p.IsActive = 0, p.UpdatedAt = @Now
    OUTPUT N'LevelsDeactivated' INTO @Counts(Step)
    FROM qalam_prod.teacher.TeacherLevels p
    WHERE p.IsActive = 1 AND NOT EXISTS (SELECT 1 FROM #StgLevels s WHERE s.Code = p.Code);

    -- 2) Markets
    UPDATE p SET p.NameEn = s.NameEn, p.NameAr = s.NameAr, p.Currency = s.Currency, p.IsActive = s.IsActive,
                 p.IsDefault = s.IsDefault, p.ExchangeRateFromBase = s.ExchangeRateFromBase, p.UpdatedAt = @Now
    OUTPUT N'MarketsUpdated' INTO @Counts(Step)
    FROM qalam_prod.pricing.PricingMarkets p JOIN #StgMarkets s ON s.Code = p.Code
    WHERE s.ExchangeRateFromBase <> p.ExchangeRateFromBase OR s.IsActive <> p.IsActive
       OR s.IsDefault <> p.IsDefault OR s.Currency <> p.Currency
       OR s.NameEn <> p.NameEn OR s.NameAr <> p.NameAr;

    INSERT qalam_prod.pricing.PricingMarkets (Code, NameEn, NameAr, Currency, IsActive, IsDefault, ExchangeRateFromBase, CreatedAt)
    OUTPUT N'MarketsInserted' INTO @Counts(Step)
    SELECT s.Code, s.NameEn, s.NameAr, s.Currency, s.IsActive, s.IsDefault, s.ExchangeRateFromBase, @Now
    FROM #StgMarkets s
    WHERE NOT EXISTS (SELECT 1 FROM qalam_prod.pricing.PricingMarkets p WHERE p.Code = s.Code);

    UPDATE p SET p.IsActive = 0, p.IsDefault = 0, p.UpdatedAt = @Now
    OUTPUT N'MarketsDeactivated' INTO @Counts(Step)
    FROM qalam_prod.pricing.PricingMarkets p
    WHERE p.IsActive = 1 AND NOT EXISTS (SELECT 1 FROM #StgMarkets s WHERE s.Code = p.Code);

    -- 3) Target base (sa) rates: staging price where the domain code matches, otherwise current prod price
    INSERT #Target (DomainId, MarketCode, SessionTypeCode, PricePerHour)
    SELECT d.Id, @Base, s.SessionTypeCode, s.PricePerHour
    FROM #StgBase s
    JOIN qalam_prod.education.EducationDomains d ON d.Code = s.DomainCode;

    INSERT #Target (DomainId, MarketCode, SessionTypeCode, PricePerHour)
    SELECT p.DomainId, @Base, p.SessionTypeCode, p.PricePerHour
    FROM qalam_prod.pricing.DomainSessionPrices p
    WHERE p.MarketCode = @Base AND p.IsActive = 1 AND p.EffectiveTo IS NULL
      AND NOT EXISTS (SELECT 1 FROM #Target t
                      WHERE t.DomainId = p.DomainId AND t.SessionTypeCode = p.SessionTypeCode);

    -- 4) Derived rates for every active non-base market
    INSERT #Target (DomainId, MarketCode, SessionTypeCode, PricePerHour)
    SELECT b.DomainId, m.Code, b.SessionTypeCode, ROUND(b.PricePerHour * m.ExchangeRateFromBase, 2)
    FROM #Target b
    CROSS JOIN qalam_prod.pricing.PricingMarkets m
    WHERE b.MarketCode = @Base AND m.IsActive = 1 AND m.Code <> @Base;

    -- 5) Diff against current open rows, close old, insert new
    INSERT #Changed (DomainId, MarketCode, SessionTypeCode, OldPrice, NewPrice)
    SELECT t.DomainId, t.MarketCode, t.SessionTypeCode, c.PricePerHour, t.PricePerHour
    FROM #Target t
    OUTER APPLY (
        SELECT TOP 1 p.PricePerHour
        FROM qalam_prod.pricing.DomainSessionPrices p
        WHERE p.DomainId = t.DomainId AND p.MarketCode = t.MarketCode AND p.SessionTypeCode = t.SessionTypeCode
          AND p.IsActive = 1 AND p.EffectiveTo IS NULL
        ORDER BY p.EffectiveFrom DESC, p.Id DESC
    ) c
    WHERE c.PricePerHour IS NULL OR c.PricePerHour <> t.PricePerHour;

    UPDATE p SET p.EffectiveTo = @Now, p.UpdatedAt = @Now
    OUTPUT N'RatesClosed' INTO @Counts(Step)
    FROM qalam_prod.pricing.DomainSessionPrices p
    JOIN #Changed c ON c.DomainId = p.DomainId AND c.MarketCode = p.MarketCode AND c.SessionTypeCode = p.SessionTypeCode
    WHERE p.EffectiveTo IS NULL;

    INSERT qalam_prod.pricing.DomainSessionPrices
        (MarketCode, DomainId, SessionTypeCode, PricePerHour, EffectiveFrom, EffectiveTo, IsActive, CreatedAt)
    OUTPUT N'RatesInserted' INTO @Counts(Step)
    SELECT MarketCode, DomainId, SessionTypeCode, NewPrice, @Now, NULL, 1, @Now
    FROM #Changed;

    PRINT '--- Rate changes (domain / market / session type) ---';
    SELECT d.Code AS DomainCode, c.MarketCode, c.SessionTypeCode, c.OldPrice, c.NewPrice
    FROM #Changed c JOIN qalam_prod.education.EducationDomains d ON d.Id = c.DomainId
    ORDER BY d.Code, CASE WHEN c.MarketCode = @Base THEN 0 ELSE 1 END, c.MarketCode, c.SessionTypeCode;

    SELECT CASE WHEN @DryRun = 1 THEN 'DRY RUN - rolled back' ELSE 'APPLIED - committed' END AS Result,
           Step, COUNT(*) AS Rows
    FROM @Counts
    GROUP BY Step
    ORDER BY Step;
    IF NOT EXISTS (SELECT 1 FROM @Counts)
        SELECT 'No changes - prod already matches staging' AS Result;

    IF @DryRun = 1
        ROLLBACK TRANSACTION;
    ELSE
        COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
