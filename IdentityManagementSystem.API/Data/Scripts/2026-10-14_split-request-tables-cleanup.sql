-- =====================================================================================================
-- پاک‌سازی بعد از 2026-10-14_split-request-tables.sql — فقط وقتی اجرا کنید که:
--   1) سیستم با ساختار جدید درست کار می‌کنه، و
--   2) POST /api/Request/BackfillEncryption (با کاربر ادمین) اجرا شده.
--
-- کارها: حذف ستون‌های plaintext قدیمی از Define.RequestWareReciept و حذف جدول پشتیبان
-- Define.Request_Legacy_20261014. اگه هنوز ردیفی هست که plaintext داره ولی Enc نداره، هیچ کاری انجام
-- نمی‌شه (تا داده‌ای از دست نره). این تغییر برگشت‌ناپذیره — قبلش از دیتابیس backup بگیرید.
--
-- ⚠️ هم‌زمان با اجرای این اسکریپت، ستون‌های legacy (NationalId/MobileNumber/VerificationCode/
-- WarehouseReceiptNumber) باید از کلاس RequestWareReciept تو Models.cs و از fallbackهای کد حذف بشن،
-- وگرنه EF دنبال ستون‌های حذف‌شده می‌گرده و همه‌ی کوئری‌های قبض انبار خطا می‌دن.
--
-- اجرا: sqlcmd -S <server> -d <db> -E -f 65001 -b -i این‌فایل
-- =====================================================================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'Define.RequestWareReciept') IS NULL
        THROW 50101, N'اسکریپت تفکیک جدول هنوز اجرا نشده است.', 1;

    IF COL_LENGTH('Define.RequestWareReciept', 'MobileNumber') IS NOT NULL
    BEGIN
        DECLARE @unencrypted INT;
        EXEC sp_executesql N'
            SELECT @c = COUNT(*) FROM Define.RequestWareReciept
            WHERE (NationalId IS NOT NULL AND NationalIdEnc IS NULL)
               OR (MobileNumber IS NOT NULL AND MobileNumberEnc IS NULL)
               OR (VerificationCode IS NOT NULL AND VerificationCodeEnc IS NULL)
               OR (WarehouseReceiptNumber IS NOT NULL AND WarehouseReceiptNumberEnc IS NULL);',
            N'@c INT OUTPUT', @c = @unencrypted OUTPUT;

        IF @unencrypted > 0
            THROW 50102, N'هنوز ردیف رمزنگاری‌نشده وجود دارد؛ ابتدا BackfillEncryption را اجرا کنید. هیچ تغییری اعمال نشد.', 1;

        ALTER TABLE Define.RequestWareReciept DROP COLUMN NationalId, MobileNumber, VerificationCode, WarehouseReceiptNumber;
    END

    IF OBJECT_ID(N'Define.Request_Legacy_20261014') IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE referenced_object_id = OBJECT_ID(N'Define.Request_Legacy_20261014'))
            THROW 50103, N'هنوز FK به جدول پشتیبان وجود دارد؛ هیچ تغییری اعمال نشد.', 1;

        DROP TABLE Define.Request_Legacy_20261014;
    END

    COMMIT TRANSACTION;
    PRINT N'پاک‌سازی انجام شد.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
