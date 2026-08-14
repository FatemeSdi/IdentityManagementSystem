-- ستون ViewedAt از WF.CartableItem گم شده بود (احتمالاً موقع یکی از تغییرات دستی قبلی حذف شده) ولی
-- مدل EF هنوز بهش نیاز داشت — همین باعث «Invalid column name 'ViewedAt'» و خطای 500 موقع ثبت درخواست می‌شد.

ALTER TABLE WF.CartableItem ADD ViewedAt DATETIME2 NULL;
GO
