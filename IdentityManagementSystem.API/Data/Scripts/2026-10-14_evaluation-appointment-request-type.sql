-- نوع درخواست دوم: «نوبت‌دهی ارزیابی». متقاضی فقط کد ملی/موبایل (+OTP) و شرکتِ محوطه رو می‌ده؛
-- درخواست بر اساس (نوع درخواست + شرکت) تو گروه کارشناس‌های نوبت‌دهی ارزیابی همون شرکت می‌شینه.
--
-- چون حالا برای هر شرکت بیش از یک گروه داریم (قبض انبار - افق / نوبت دهی ارزیابی - افق)، گروه
-- علاوه بر CompanyId باید RequestTypeId هم داشته باشه تا موقع ثبت درخواست گروه درست انتخاب بشه.
-- گروه‌های «نوبت دهی ارزیابی - افق/بتا» (Id=4,5) از قبل دستی ساخته شده بودن.
--
-- اسکریپت idempotent‌ه (اجرای دوباره‌ش مشکلی نداره).
-- اجرا: sqlcmd -S <server> -d <db> -E -f 65001 -i این‌فایل (UTF-8، شامل متن فارسی)

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1) کارتابل نوبت‌دهی ارزیابی (به نقش کارشناس = 1 وصل)
IF NOT EXISTS (SELECT 1 FROM WF.Cartable WHERE CartableName = N'کارتابل کارشناس نوبت‌دهی ارزیابی')
    INSERT INTO WF.Cartable (CartableName, CreatedAt)
    VALUES (N'کارتابل کارشناس نوبت‌دهی ارزیابی', SYSUTCDATETIME());

INSERT INTO WF.CartableRoles (CartableId, RoleId, CreatedAt)
SELECT c.CartableId, 1, SYSUTCDATETIME()
FROM WF.Cartable c
WHERE c.CartableName = N'کارتابل کارشناس نوبت‌دهی ارزیابی'
  AND NOT EXISTS (SELECT 1 FROM WF.CartableRoles cr WHERE cr.CartableId = c.CartableId AND cr.RoleId = 1);

-- 2) نوع درخواست
INSERT INTO Define.RequestType (TypeName, Code, CartableId, IsActive)
SELECT N'نوبت‌دهی ارزیابی', N'EVALUATION_APPOINTMENT', c.CartableId, 1
FROM WF.Cartable c
WHERE c.CartableName = N'کارتابل کارشناس نوبت‌دهی ارزیابی'
  AND NOT EXISTS (SELECT 1 FROM Define.RequestType WHERE Code = N'EVALUATION_APPOINTMENT');
GO

-- 3) ستون RequestTypeId روی گروه‌ها
IF COL_LENGTH('Sec.[Group]', 'RequestTypeId') IS NULL
    ALTER TABLE Sec.[Group] ADD RequestTypeId INT NULL;
GO
IF OBJECT_ID('Sec.FK_Group_RequestType') IS NULL
    ALTER TABLE Sec.[Group] ADD CONSTRAINT FK_Group_RequestType
        FOREIGN KEY (RequestTypeId) REFERENCES Define.RequestType(RequestTypeId);
GO

-- 4) وصل کردن گروه‌ها به نوعشون
UPDATE Sec.[Group]
SET RequestTypeId = (SELECT RequestTypeId FROM Define.RequestType WHERE Code = N'WAREHOUSE_RECEIPT')
WHERE CompanyId IS NOT NULL AND Title LIKE N'قبض انبار%';

UPDATE Sec.[Group]
SET RequestTypeId = (SELECT RequestTypeId FROM Define.RequestType WHERE Code = N'EVALUATION_APPOINTMENT')
WHERE CompanyId IS NOT NULL AND Title LIKE N'نوبت%ارزیابی%';
GO
