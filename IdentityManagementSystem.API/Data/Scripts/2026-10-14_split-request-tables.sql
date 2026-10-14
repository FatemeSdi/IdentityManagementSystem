-- =====================================================================================================
-- تفکیک جدول درخواست بر اساس نوع:
--   Define.Request            → فقط ستون‌های مشترک (کد پیگیری، نوع، شرکت، گروه، take، وضعیت، ممیزی)
--   Define.RequestWareReciept → اطلاعات مخصوص قبض انبار (سند، رمز تصدیق، شماره قبض، نتایج احراز)
--   Define.RequestEval        → اطلاعات مخصوص نوبت‌دهی ارزیابی (کد ملی، موبایل، احراز، نوبت تعیین‌شده)
--   Define.EvalAppointmentSlot→ نوبت‌های خالی هر محوطه (شرکت) که ادمین تعریف می‌کنه
--
-- داده‌های حساس فقط به‌صورت Enc (AES) + Hash (HMAC، برای جستجو) ذخیره می‌شن. ستون‌های plaintext
-- RequestWareReciept فقط برای رکوردهای قدیمیِ قبل از رمزنگاری هستن (fallback)؛ رکورد جدید هیچ‌وقت پرشون نمی‌کنه.
--
-- ایمنی:
--   * کل اسکریپت یک batch و یک تراکنشه — هر خطایی → ROLLBACK کامل، دیتابیس دست‌نخورده می‌مونه.
--   * هیچ داده‌ای حذف نمی‌شه: جدول فعلی به Define.Request_Legacy_20261014 تغییر نام پیدا می‌کنه و به‌عنوان
--     نسخه‌ی پشتیبان می‌مونه (حذفش با اسکریپت جداگانه، بعد از اطمینان).
--   * ردیف‌های فعلی با همون RequestId منتقل می‌شن، پس ۹ جدولی که به Request FK دارن (کارتابل، تاریخچه،
--     لاگ پیامک/شاهکار/سند، قبض انبار، ...) بدون تغییر داده معتبر می‌مونن؛ فقط FKشون به جدول جدید وصل می‌شه.
--   * قبل از COMMIT تعداد ردیف‌ها چک می‌شه.
--   * اگه قبلاً اجرا شده باشه، بدون هیچ تغییری خطا می‌ده.
--
-- اجرا: sqlcmd -S <server> -d <db> -E -f 65001 -b -i این‌فایل   (یا کل فایل در SSMS — بدون انتخاب بخشی ازش)
-- بعد از اجرا: API رو ری‌استارت کنید و یک‌بار POST /api/Request/BackfillEncryption (با کاربر ادمین) صدا بزنید
-- تا شماره موبایل رکوردهای قدیمی قبض انبار هم رمزنگاری بشه.
-- =====================================================================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'Define.RequestWareReciept') IS NOT NULL OR OBJECT_ID(N'Define.Request_Legacy_20261014') IS NOT NULL
        THROW 50001, N'این اسکریپت قبلاً اجرا شده است؛ هیچ تغییری اعمال نشد.', 1;

    DECLARE @WareTypeId INT = (SELECT RequestTypeId FROM Define.RequestType WHERE Code = N'WAREHOUSE_RECEIPT');
    DECLARE @EvalTypeId INT = (SELECT RequestTypeId FROM Define.RequestType WHERE Code = N'EVALUATION_APPOINTMENT');
    IF @WareTypeId IS NULL OR @EvalTypeId IS NULL
        THROW 50002, N'نوع درخواست WAREHOUSE_RECEIPT یا EVALUATION_APPOINTMENT تعریف نشده است.', 1;

    IF EXISTS (SELECT 1 FROM Define.Request WHERE RequestTypeId IS NULL OR RequestTypeId NOT IN (@WareTypeId, @EvalTypeId))
        THROW 50003, N'درخواستی با نوع نامشخص وجود دارد؛ انتقال انجام نشد.', 1;

    DECLARE @LegacyCount INT = (SELECT COUNT(*) FROM Define.Request);

    -- 1) جدول فعلی → پشتیبان. FKهای خروجیش حذف می‌شن چون اسمشون (FK_Request_*) برای جدول جدید لازمه.
    EXEC sp_rename N'Define.Request', N'Request_Legacy_20261014';
    ALTER TABLE Define.Request_Legacy_20261014 DROP CONSTRAINT FK_Request_Group, FK_Request_AssignedTo, FK_Request_Company, FK_Request_RequestType;

    -- 2) جدول پایه‌ی جدید
    EXEC(N'
    CREATE TABLE Define.Request (
        RequestId        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Request PRIMARY KEY,
        RequestCode      NVARCHAR(50)  NOT NULL,
        TrackingCode     NVARCHAR(30)  NULL,
        RequestTypeId    INT           NOT NULL CONSTRAINT FK_Request_RequestType REFERENCES Define.RequestType(RequestTypeId),
        CompanyId        INT           NULL     CONSTRAINT FK_Request_Company     REFERENCES Define.Company(CompanyId),
        GroupId          INT           NULL     CONSTRAINT FK_Request_Group       REFERENCES Sec.[Group](Id),
        AssignedTo       BIGINT        NULL     CONSTRAINT FK_Request_AssignedTo  REFERENCES Sec.[User](UserId),
        AssignedAt       DATETIME2     NULL,
        ValidateByExpert BIT           NULL,
        Description      NVARCHAR(500) NULL,
        CreatedAt        DATETIME2     NOT NULL CONSTRAINT DF_Request_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy        NVARCHAR(100) NULL,
        UpdatedAt        DATETIME2     NULL,
        UpdatedBy        NVARCHAR(100) NULL
    );
    CREATE UNIQUE INDEX UX_Request_TrackingCode ON Define.Request(TrackingCode) WHERE TrackingCode IS NOT NULL;
    CREATE INDEX IX_Request_GroupId ON Define.Request(GroupId);
    CREATE INDEX IX_Request_AssignedTo ON Define.Request(AssignedTo);
    CREATE INDEX IX_Request_RequestCode ON Define.Request(RequestCode);
    CREATE INDEX IX_Request_CreatedAt ON Define.Request(CreatedAt);
    ');

    -- 3) جزئیات قبض انبار
    EXEC(N'
    CREATE TABLE Define.RequestWareReciept (
        RequestId                  BIGINT        NOT NULL CONSTRAINT PK_RequestWareReciept PRIMARY KEY
                                                          CONSTRAINT FK_RequestWareReciept_Request REFERENCES Define.Request(RequestId),
        DocumentNumber             NVARCHAR(50)  NULL,
        IsMatch                    BIT           NULL,
        IsExist                    BIT           NULL,
        IsNationalIdInResponse     BIT           NULL,
        IsNationalIdInLawyers      BIT           NULL,
        NationalIdEnc              NVARCHAR(256) NULL,
        NationalIdHash             CHAR(64)      NULL,
        MobileNumberEnc            NVARCHAR(256) NULL,
        MobileNumberHash           CHAR(64)      NULL,
        VerificationCodeEnc        NVARCHAR(256) NULL,
        VerificationCodeHash       CHAR(64)      NULL,
        WarehouseReceiptNumberEnc  NVARCHAR(256) NULL,
        WarehouseReceiptNumberHash CHAR(64)      NULL,
        -- فقط رکوردهای قدیمی (fallback)؛ رکورد جدید هیچ‌وقت این ستون‌ها رو پر نمی‌کنه
        NationalId                 NVARCHAR(20)  NULL,
        MobileNumber               NVARCHAR(20)  NULL,
        VerificationCode           NVARCHAR(50)  NULL,
        WarehouseReceiptNumber     NVARCHAR(50)  NULL
    );
    CREATE INDEX IX_RequestWareReciept_NationalIdHash ON Define.RequestWareReciept(NationalIdHash);
    CREATE INDEX IX_RequestWareReciept_MobileNumberHash ON Define.RequestWareReciept(MobileNumberHash);
    CREATE INDEX IX_RequestWareReciept_WarehouseReceiptNumberHash ON Define.RequestWareReciept(WarehouseReceiptNumberHash);
    CREATE INDEX IX_RequestWareReciept_DocumentNumber ON Define.RequestWareReciept(DocumentNumber);
    ');

    -- 4) نوبت‌های خالی هر محوطه (شرکت) — StartAt به UTC
    EXEC(N'
    CREATE TABLE Define.EvalAppointmentSlot (
        SlotId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EvalAppointmentSlot PRIMARY KEY,
        CompanyId       INT           NOT NULL CONSTRAINT FK_EvalAppointmentSlot_Company REFERENCES Define.Company(CompanyId),
        StartAt         DATETIME2     NOT NULL,
        DurationMinutes INT           NOT NULL CONSTRAINT CK_EvalAppointmentSlot_Duration CHECK (DurationMinutes > 0),
        Capacity        INT           NOT NULL CONSTRAINT DF_EvalAppointmentSlot_Capacity DEFAULT 1
                                               CONSTRAINT CK_EvalAppointmentSlot_Capacity CHECK (Capacity > 0),
        IsActive        BIT           NOT NULL CONSTRAINT DF_EvalAppointmentSlot_IsActive DEFAULT 1,
        CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_EvalAppointmentSlot_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy       NVARCHAR(100) NULL,
        CONSTRAINT UQ_EvalAppointmentSlot_Company_StartAt UNIQUE (CompanyId, StartAt)
    );
    ');

    -- 5) جزئیات نوبت‌دهی ارزیابی
    EXEC(N'
    CREATE TABLE Define.RequestEval (
        RequestId        BIGINT        NOT NULL CONSTRAINT PK_RequestEval PRIMARY KEY
                                                CONSTRAINT FK_RequestEval_Request REFERENCES Define.Request(RequestId),
        NationalIdEnc    NVARCHAR(256) NULL,
        NationalIdHash   CHAR(64)      NULL,
        MobileNumberEnc  NVARCHAR(256) NULL,
        MobileNumberHash CHAR(64)      NULL,
        IsMatch          BIT           NULL,
        SlotId           INT           NULL CONSTRAINT FK_RequestEval_Slot REFERENCES Define.EvalAppointmentSlot(SlotId),
        AppointmentSetBy BIGINT        NULL CONSTRAINT FK_RequestEval_AppointmentSetBy REFERENCES Sec.[User](UserId),
        AppointmentSetAt DATETIME2     NULL
    );
    CREATE INDEX IX_RequestEval_NationalIdHash ON Define.RequestEval(NationalIdHash);
    CREATE INDEX IX_RequestEval_MobileNumberHash ON Define.RequestEval(MobileNumberHash);
    CREATE INDEX IX_RequestEval_SlotId ON Define.RequestEval(SlotId);
    ');

    -- 6) انتقال داده با همون RequestId
    EXEC(N'
    SET IDENTITY_INSERT Define.Request ON;
    INSERT INTO Define.Request (RequestId, RequestCode, TrackingCode, RequestTypeId, CompanyId, GroupId, AssignedTo, AssignedAt,
                                ValidateByExpert, Description, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
    SELECT RequestId, RequestCode, TrackingCode, RequestTypeId, CompanyId, GroupId, AssignedTo, AssignedAt,
           ValidateByExpert, Description, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
    FROM Define.Request_Legacy_20261014;
    SET IDENTITY_INSERT Define.Request OFF;
    ');

    EXEC sp_executesql N'
    INSERT INTO Define.RequestWareReciept (RequestId, DocumentNumber, IsMatch, IsExist, IsNationalIdInResponse, IsNationalIdInLawyers,
                                           NationalIdEnc, NationalIdHash, VerificationCodeEnc, VerificationCodeHash,
                                           WarehouseReceiptNumberEnc, WarehouseReceiptNumberHash,
                                           NationalId, MobileNumber, VerificationCode, WarehouseReceiptNumber)
    SELECT RequestId, DocumentNumber, IsMatch, IsExist, IsNationalIdInResponse, IsNationalIdInLawyers,
           NationalIdEnc, NationalIdHash, VerificationCodeEnc, VerificationCodeHash,
           WarehouseReceiptNumberEnc, WarehouseReceiptNumberHash,
           NationalId, MobileNumber, VerificationCode, WarehouseReceiptNumber
    FROM Define.Request_Legacy_20261014
    WHERE RequestTypeId = @WareTypeId;',
        N'@WareTypeId INT', @WareTypeId = @WareTypeId;

    -- شماره موبایل درخواست‌های ارزیابیِ قبلی (تستی) فقط plaintext بوده و SQL نمی‌تونه رمزنگاری AES اپلیکیشن رو
    -- انجام بده؛ پس منتقل نمی‌شه (تو جدول پشتیبان باقی می‌مونه). این ردیف‌ها با موبایل قابل پیگیری نیستن.
    EXEC sp_executesql N'
    INSERT INTO Define.RequestEval (RequestId, NationalIdEnc, NationalIdHash, IsMatch)
    SELECT RequestId, NationalIdEnc, NationalIdHash, IsMatch
    FROM Define.Request_Legacy_20261014
    WHERE RequestTypeId = @EvalTypeId;',
        N'@EvalTypeId INT', @EvalTypeId = @EvalTypeId;

    -- 7) FKهای ۹ جدول وابسته (که با sp_rename دنبال جدول پشتیبان رفتن) → جدول جدید Define.Request
    DECLARE @fkName SYSNAME, @childTable NVARCHAR(300), @childCol SYSNAME, @sql NVARCHAR(MAX);
    DECLARE fk_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT fk.name,
               QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id)),
               COL_NAME(fc.parent_object_id, fc.parent_column_id)
        FROM sys.foreign_keys fk
        JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = fk.object_id
        WHERE fk.referenced_object_id = OBJECT_ID(N'Define.Request_Legacy_20261014');
    OPEN fk_cursor;
    FETCH NEXT FROM fk_cursor INTO @fkName, @childTable, @childCol;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = N'ALTER TABLE ' + @childTable + N' DROP CONSTRAINT ' + QUOTENAME(@fkName) + N'; '
                 + N'ALTER TABLE ' + @childTable + N' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(@fkName)
                 + N' FOREIGN KEY (' + QUOTENAME(@childCol) + N') REFERENCES Define.Request(RequestId);';
        EXEC sp_executesql @sql;
        FETCH NEXT FROM fk_cursor INTO @fkName, @childTable, @childCol;
    END
    CLOSE fk_cursor;
    DEALLOCATE fk_cursor;

    -- 8) کنترل نهایی
    DECLARE @BaseCount INT, @ChildCount INT, @RemainingFks INT;
    EXEC sp_executesql N'SELECT @c = COUNT(*) FROM Define.Request', N'@c INT OUTPUT', @c = @BaseCount OUTPUT;
    EXEC sp_executesql N'SELECT @c = (SELECT COUNT(*) FROM Define.RequestWareReciept) + (SELECT COUNT(*) FROM Define.RequestEval)', N'@c INT OUTPUT', @c = @ChildCount OUTPUT;
    SET @RemainingFks = (SELECT COUNT(*) FROM sys.foreign_keys WHERE referenced_object_id = OBJECT_ID(N'Define.Request_Legacy_20261014'));

    IF @BaseCount <> @LegacyCount OR @ChildCount <> @LegacyCount OR @RemainingFks <> 0
        THROW 50004, N'کنترل نهایی تعداد ردیف‌ها/FKها ناموفق بود؛ همه‌چیز برگردانده شد.', 1;

    COMMIT TRANSACTION;
    PRINT N'انجام شد: ' + CAST(@LegacyCount AS NVARCHAR(20)) + N' درخواست منتقل شد؛ جدول قبلی به‌عنوان Define.Request_Legacy_20261014 نگه داشته شد.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
