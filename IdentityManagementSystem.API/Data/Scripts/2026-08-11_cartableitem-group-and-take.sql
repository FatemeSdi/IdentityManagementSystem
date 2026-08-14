-- CartableItem الان فقط موقع Take یه ردیف می‌گیره. از این به بعد، همون لحظه‌ی ثبت درخواست هم
-- یه ردیف «تو صفِ گروه» براش ساخته می‌شه (CartableId هنوز NULL چون به هیچ کارتابل شخصی‌ای وصل نیست)،
-- و وقتی یکی take می‌کنه همون ردیف آپدیت می‌شه (نه ردیف جدید). برای همین CartableId باید نال‌پذیر بشه.
--
-- اجرا: sqlcmd -S <server> -d <db> -E -f 65001 -i این‌فایل (فایل UTF-8 با متن فارسیه)

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

ALTER TABLE WF.CartableItem ALTER COLUMN CartableId BIGINT NULL;
GO

-- گروهی که این آیتم بهش تعلق داره (مستقل از اینکه هنوز کسی take کرده باشه یا نه)
ALTER TABLE WF.CartableItem ADD GroupId INT NULL;
GO
ALTER TABLE WF.CartableItem ADD CONSTRAINT FK_CartableItem_Group FOREIGN KEY (GroupId) REFERENCES Sec.[Group](Id);
GO

-- دقیقاً چه کسی take کرده (صریح، جدا از AssignedTo که تاریخچه‌ی assign دستیِ متصدی هم هست)
ALTER TABLE WF.CartableItem ADD IsTakenBy BIGINT NULL;
GO
ALTER TABLE WF.CartableItem ADD CONSTRAINT FK_CartableItem_IsTakenBy FOREIGN KEY (IsTakenBy) REFERENCES Sec.[User](UserId);
GO

ALTER TABLE WF.CartableItem ADD UpdatedAt DATETIME2 NULL;
GO

-- بک‌فیل سه ردیفی که از قبل هست (همه‌شون قبلاً take شدن)
UPDATE ci
SET ci.GroupId = r.GroupId,
    ci.IsTakenBy = ci.AssignedTo,
    ci.UpdatedAt = ci.AssignedAt
FROM WF.CartableItem ci
JOIN Define.Request r ON r.RequestId = ci.RequestId;
GO
