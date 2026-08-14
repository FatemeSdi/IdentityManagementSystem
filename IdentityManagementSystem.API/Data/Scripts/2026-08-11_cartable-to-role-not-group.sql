-- Cartable باید به Role وصل باشه (از طریق CartableRoles)، نه به Group. جداسازی شرکت‌ها همون‌طور که
-- قبلاً درست ساخته شده از طریق CartableItem.GroupId + Request.GroupId/UserGroup انجام می‌شه — کاملاً
-- مستقل از اینکه CartableItem به کدوم Cartable وصله. برای همین دو ردیفِ per-group که قبلاً ساخته شده
-- بودن (5=افق، 6=بتا) معنی ندارن؛ همه باید به همون یه Cartable مشترکِ «کارشناس قبض انبار» (Id=1) وصل بشن.
--
-- اجرا: sqlcmd -S <server> -d <db> -E -f 65001 -i این‌فایل (UTF-8، شامل متن فارسی)

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

UPDATE ci SET ci.CartableId = 1 FROM WF.CartableItem ci WHERE ci.CartableId IN (5, 6);
GO

DELETE FROM WF.CartableRoles WHERE CartableId IN (5, 6);
GO

DELETE FROM WF.Cartable WHERE CartableId IN (5, 6);
GO

UPDATE WF.Cartable SET CartableName = N'کارتابل کارشناس قبض انبار' WHERE CartableId = 1;
GO

INSERT INTO WF.CartableRoles (CartableId, RoleId, CreatedAt)
SELECT 1, 1, SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM WF.CartableRoles WHERE CartableId = 1 AND RoleId = 1);
GO

-- GroupId روی Cartable دیگه معنی نداره (Cartable الان فقط به Role وصله)
ALTER TABLE WF.Cartable DROP CONSTRAINT FK_Cartable_Group;
GO
DROP INDEX UQ_Cartable_GroupId ON WF.Cartable;
GO
ALTER TABLE WF.Cartable DROP COLUMN GroupId;
GO

-- AssignedAt اولیه (قبل از take شدن) باید تاریخ ثبت درخواست باشه، نه NULL
UPDATE ci
SET ci.AssignedAt = r.CreatedAt
FROM WF.CartableItem ci
JOIN Define.Request r ON r.RequestId = ci.RequestId
WHERE ci.AssignedAt IS NULL;
GO
