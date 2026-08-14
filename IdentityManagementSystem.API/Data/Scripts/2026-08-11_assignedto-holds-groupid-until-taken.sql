-- AssignedTo دیگه فقط UserId نگه نمی‌داره — قبل از اینکه کسی take کنه، همون GroupId ای که آیتم
-- بهش تعلق داره رو نگه می‌داره (یعنی «الان تو کارتابل کدوم گروهه»)؛ وقتی take شد، با UserId کارشناسی
-- که take کرده جایگزین می‌شه. برای همین دیگه نمی‌شه FK واقعی به Sec.User روش داشت (بعضی وقتا GroupId
-- نگه می‌داره)، و ستون جداگانه‌ی GroupId هم دیگه لازم نیست چون همون اطلاعات تو AssignedTo هست.
--
-- اجرا: sqlcmd -S <server> -d <db> -E -f 65001 -i این‌فایل (UTF-8، شامل متن فارسی)

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

ALTER TABLE WF.CartableItem DROP CONSTRAINT FK_cartableItems_users;
GO

-- ردیف‌هایی که هنوز کسی take نکرده (AssignedTo هنوز NULL بود) مقدار GroupId‌شون رو می‌گیرن
UPDATE WF.CartableItem
SET AssignedTo = GroupId
WHERE AssignedTo IS NULL AND GroupId IS NOT NULL;
GO

ALTER TABLE WF.CartableItem DROP CONSTRAINT FK_CartableItem_Group;
GO
ALTER TABLE WF.CartableItem DROP COLUMN GroupId;
GO
