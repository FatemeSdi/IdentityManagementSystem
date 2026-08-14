-- Define.RequestType: نوع درخواست (فعلاً فقط «قبض انبار»، در آینده «نوبت‌دهی ارزیابی» و بقیه هم اضافه می‌شن).
-- هر نوع درخواست به یه Cartable وصله (WF.Cartable) — یعنی همون لحظه‌ی ثبت درخواست، بر اساس نوعش،
-- مشخصه باید بره تو کدوم کارتابل؛ دیگه لازم نیست صبر کنیم کسی take کنه تا CartableId پر بشه.
--
-- اجرا: sqlcmd -S <server> -d <db> -E -f 65001 -i این‌فایل (UTF-8، شامل متن فارسی)

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

CREATE TABLE Define.RequestType (
    RequestTypeId INT IDENTITY(1,1) PRIMARY KEY,
    TypeName NVARCHAR(100) NOT NULL,
    Code NVARCHAR(50) NULL,
    CartableId BIGINT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

ALTER TABLE Define.RequestType ADD CONSTRAINT FK_RequestType_Cartable FOREIGN KEY (CartableId) REFERENCES WF.Cartable(CartableId);
GO
CREATE UNIQUE INDEX UQ_RequestType_Code ON Define.RequestType(Code) WHERE Code IS NOT NULL;
GO

INSERT INTO Define.RequestType (TypeName, Code, CartableId, IsActive)
VALUES (N'قبض انبار', N'WAREHOUSE_RECEIPT', 1, 1);
GO

ALTER TABLE Define.Request ADD RequestTypeId INT NULL;
GO
ALTER TABLE Define.Request ADD CONSTRAINT FK_Request_RequestType FOREIGN KEY (RequestTypeId) REFERENCES Define.RequestType(RequestTypeId);
GO

-- همه‌ی درخواست‌های فعلی (چه قدیمی چه جدید) از نوع قبض انبار بودن — چون فعلاً تنها نوعِ موجوده
UPDATE Define.Request
SET RequestTypeId = (SELECT RequestTypeId FROM Define.RequestType WHERE Code = N'WAREHOUSE_RECEIPT')
WHERE RequestTypeId IS NULL;
GO

-- ردیف‌های CartableItem که هنوز take نشدن و CartableId‌شون خالی مونده رو هم بر اساس نوع درخواست‌شون پر می‌کنیم
UPDATE ci
SET ci.CartableId = rt.CartableId
FROM WF.CartableItem ci
JOIN Define.Request r ON r.RequestId = ci.RequestId
JOIN Define.RequestType rt ON rt.RequestTypeId = r.RequestTypeId
WHERE ci.CartableId IS NULL;
GO
