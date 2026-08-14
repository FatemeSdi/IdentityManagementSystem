-- RoleId=1 اسمش «کارشناس حراست» بود ولی فقط و فقط توسط ۴ کارشناس قبض انبار استفاده می‌شه
-- (Fa-sadeghi, m-sadeghi, z.fouladi, a-vatanchi) — اسم‌گذاری اشتباه بوده، اصلاحش می‌کنیم.
--
-- بعدش، به‌جای اینکه کد runtime هر بار یه Cartable تازه بسازه، رابطه‌ی WF.CartableRoles رو
-- (که دقیقاً برای همین ساخته شده بود) پر می‌کنیم: کارتابل‌های موجودِ قبض انبار به Role
-- «کارشناس قبض انبار» وصل می‌شن.
--
-- اجرا: sqlcmd -S <server> -d <db> -E -f 65001 -i این‌فایل (UTF-8، شامل متن فارسی)

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

UPDATE Sec.Role SET RoleName = N'کارشناس قبض انبار', UpdatedAt = SYSUTCDATETIME() WHERE RoleId = 1;
GO

INSERT INTO WF.CartableRoles (CartableId, RoleId, CreatedAt)
SELECT c.CartableId, 1, SYSUTCDATETIME()
FROM WF.Cartable c
WHERE c.GroupId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM WF.CartableRoles cr WHERE cr.CartableId = c.CartableId AND cr.RoleId = 1);
GO
