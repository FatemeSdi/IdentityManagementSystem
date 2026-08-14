-- WF.Cartable طبق طراحی اصلی (نگاه کن به WF.CartableRoles که Cartable رو به Role وصل می‌کنه، و
-- CartableName های موجود مثل «کارتابل کارشناس حراست») قرار بوده یه TYPE/تعریفِ کارتابل باشه — نه
-- یه ردیف شخصی به‌ازای هر کاربر. کد فعلی Take اشتباه هر بار برای هر کاربر جدید یه Cartable تازه
-- می‌ساخت (UserId به‌عنوان کلید). این اسکریپت Cartable رو به «یک ردیف مشترک به‌ازای هر گروه» تغییر می‌ده
-- — یعنی همه‌ی کارشناس‌های یه شرکت (که تو همون Group ان) تو یه کارتابل مشترک کار می‌کنن.
--
-- اجرا: sqlcmd -S <server> -d <db> -E -f 65001 -i این‌فایل (UTF-8، شامل متن فارسی)

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

ALTER TABLE WF.Cartable ALTER COLUMN UserId BIGINT NULL;
GO

ALTER TABLE WF.Cartable ADD GroupId INT NULL;
GO
ALTER TABLE WF.Cartable ADD CONSTRAINT FK_Cartable_Group FOREIGN KEY (GroupId) REFERENCES Sec.[Group](Id);
GO
CREATE UNIQUE INDEX UQ_Cartable_GroupId ON WF.Cartable(GroupId) WHERE GroupId IS NOT NULL;
GO

-- یه کارتابل مشترک برای هر گروهِ موجود می‌سازیم (اگه از قبل نداره)
INSERT INTO WF.Cartable (GroupId, CartableName, CreatedAt)
SELECT g.Id, N'کارتابل ' + g.Title, SYSUTCDATETIME()
FROM Sec.[Group] g
WHERE NOT EXISTS (SELECT 1 FROM WF.Cartable c WHERE c.GroupId = g.Id);
GO

-- آیتم‌های موجود (که تا الان به کارتابل شخصیِ کاربر وصل بودن) رو به کارتابل مشترکِ همون گروه منتقل می‌کنیم
UPDATE ci
SET ci.CartableId = gc.CartableId
FROM WF.CartableItem ci
JOIN WF.Cartable gc ON gc.GroupId = ci.GroupId
WHERE ci.GroupId IS NOT NULL;
GO

-- ردیف‌های شخصیِ قدیمی (بدون GroupId، بدون نام، دیگه هیچ CartableItem ای بهشون اشاره نمی‌کنه) رو پاک می‌کنیم.
-- کارتابل‌های دستیِ قدیمی‌تر که اسم دارن (مثل «کارتابل کارشناس حراست») دست‌نخورده می‌مونن.
DELETE FROM WF.Cartable
WHERE GroupId IS NULL
  AND CartableName IS NULL
  AND NOT EXISTS (SELECT 1 FROM WF.CartableItem ci WHERE ci.CartableId = WF.Cartable.CartableId);
GO
