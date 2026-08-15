-- کد ملی، رمز تصدیق و شماره قبض انبار باید به‌صورت رمزنگاری‌شده ذخیره بشن (نه plaintext).
-- الگوی این تغییر دقیقاً همون چیزیه که قبلاً برای Sec.User / Log.ShahkarLog / Log.VerifyDocLog
-- انتخاب شده بود (ستون Enc کنار ستون قدیمی + ستون Hash برای جستجوی دقیق/blind-index) —
-- فقط اینجا واقعاً هم پر و هم خونده می‌شه. ستون‌های قدیمی plaintext دست‌نخورده می‌مونن
-- (فقط برای رکوردهای جدید دیگه پر نمی‌شن) تا هیچ داده‌ی موجودی از دست نره یا نمایش نشکنه.

ALTER TABLE Define.Request ADD
    NationalIdEnc nvarchar(256) NULL,
    NationalIdHash char(64) NULL,
    VerificationCodeEnc nvarchar(256) NULL,
    VerificationCodeHash char(64) NULL,
    WarehouseReceiptNumberEnc nvarchar(256) NULL,
    WarehouseReceiptNumberHash char(64) NULL;
GO

ALTER TABLE Define.WarehouseReceipt ADD
    OwnerNationalIdEnc nvarchar(256) NULL,
    OwnerNationalIdHash char(64) NULL,
    ReceiptNumberEnc nvarchar(256) NULL,
    ReceiptNumberHash char(64) NULL;
GO

-- ReceiptNumber الان NOT NULL هست؛ باید nullable بشه چون رکوردهای جدید فقط Enc رو پر می‌کنن
-- (دیگه مقدار plaintext اجباری نیست).
ALTER TABLE Define.WarehouseReceipt ALTER COLUMN ReceiptNumber nvarchar(50) NULL;
GO

SET QUOTED_IDENTIFIER ON;
GO
CREATE INDEX IX_Request_NationalIdHash ON Define.Request(NationalIdHash);
CREATE INDEX IX_Request_WarehouseReceiptNumberHash ON Define.Request(WarehouseReceiptNumberHash);
GO
