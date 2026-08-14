-- تفکیک کارتابل بر اساس شرکت: هر گروه (Sec.Group) به یک شرکت (Define.Company) مقید می‌شه.
-- کارشناس‌ها با UserGroup به گروهِ همون شرکت بسته می‌شن؛ درخواست‌ها موقع ثبت بر اساس شرکتِ
-- انتخابی متقاضی روی همون گروه GroupId می‌گیرن. منطق Take/Cartable فعلی بدون تغییر کار می‌کنه.
--
-- نکته ۱: جدول Define.Company از قبل تو دیتابیس وجود داشت (با FK_WarehouseReceipt_Company هم از قبل
-- بسته شده بود) ولی هیچ‌جا تو کد EF مپ نشده بود و خالی بود. اینجا فقط سیدش می‌کنیم.
--
-- نکته ۲ (اجرا): این فایل UTF-8 هست و شامل متن فارسیه. با sqlcmd باید با کدپیج UTF-8 اجرا بشه
-- وگرنه N'...' literal ها کورپت می‌شن: sqlcmd -S <server> -d <db> -E -f 65001 -i این‌فایل

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1) سید شرکت‌ها روی جدول موجود Define.Company
INSERT INTO Define.Company (CompanyName, IsActive, CreatedAt, CreatedBy)
VALUES (N'افق', 1, SYSUTCDATETIME(), N'migration'),
       (N'بتا', 1, SYSUTCDATETIME(), N'migration');
GO

-- 2) هر گروه می‌تونه به یک شرکت مقید بشه
ALTER TABLE Sec.[Group] ADD CompanyId INT NULL;
GO
ALTER TABLE Sec.[Group] ADD CONSTRAINT FK_Group_Company FOREIGN KEY (CompanyId) REFERENCES Define.Company(CompanyId);
GO

-- گروه فعلی «قبض انبار» (Id=2) مال شرکت افقه — بقیه‌ی کارشناس‌های موجود (Fa-sadeghi, z.fouladi, m-sadeghi) هم همینجان
UPDATE Sec.[Group]
SET Title = N'قبض انبار - افق', CompanyId = (SELECT CompanyId FROM Define.Company WHERE CompanyName = N'افق')
WHERE Id = 2;
GO

-- گروه جدید برای شرکت بتا
INSERT INTO Sec.[Group] (Title, CompanyId)
VALUES (N'قبض انبار - بتا', (SELECT CompanyId FROM Define.Company WHERE CompanyName = N'بتا'));
GO

-- کارشناس a-vatanchi (UserId=11) عضو گروه قبض انبار شرکت بتا می‌شه
INSERT INTO Sec.UserGroup (UserId, GroupId)
VALUES (11, (SELECT Id FROM Sec.[Group] WHERE Title = N'قبض انبار - بتا'));
GO

-- 3) درخواست‌ها شرکت انتخابی متقاضی رو نگه می‌دارن (برای گزارش‌گیری/ردیابی، مستقل از GroupId)
ALTER TABLE Define.Request ADD CompanyId INT NULL;
GO
ALTER TABLE Define.Request ADD CONSTRAINT FK_Request_Company FOREIGN KEY (CompanyId) REFERENCES Define.Company(CompanyId);
GO

-- backfill درخواست‌های قبلی از روی گروهشون (فعلاً همه قبض انبار/افق بودن)
UPDATE r
SET r.CompanyId = g.CompanyId
FROM Define.Request r
JOIN Sec.[Group] g ON g.Id = r.GroupId
WHERE r.CompanyId IS NULL;
GO
