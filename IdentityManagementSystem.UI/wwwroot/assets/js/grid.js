// ========== راه‌اندازی Grid.js ==========
// بررسی وجود gridjs
if (typeof gridjs === 'undefined') {
    console.error('Grid.js بارگذاری نشده است');
    return;
}

// پیدا کردن المان جدول
const gridContainer = document.getElementById('grid-loading');
if (!gridContainer) {
    console.error('المان grid-loading یافت نشد');
    return;
}

// پاک کردن محتوای قبلی
gridContainer.innerHTML = '';

// ایجاد Grid جدید
gridInstance = new gridjs.Grid({
    columns: [
        {
            id: 'rowNumber',
            name: 'ردیف',
            width: '80px',
            sort: false,
            formatter: (_, row) => {
                // شماره ردیف بر اساس صفحه و ایندکس
                const page = gridInstance?.config?.pagination?.currentPage || 1;
                const limit = gridInstance?.config?.pagination?.limit || 25;
                const index = gridData.indexOf(row);
                return (page - 1) * limit + index + 1;
            }
        },
        {
            id: 'date',
            name: 'تاریخ',
            width: '150px'
        },
        {
            id: 'entranceType',
            name: 'مسیر تردد',
            width: 'auto'
        },
        {
            id: 'count',
            name: 'تعداد',
            width: '100px',
            formatter: (cell) => {
                return `<span class="badge bg-success">${cell}</span>`;
            }
        }
    ],
    data: [],
    language: {
        'search': {
            'placeholder': 'جستجو...'
        },
        'pagination': {
            'previous': 'قبلی',
            'next': 'بعدی',
            'showing': 'نمایش',
            'of': 'از',
            'results': 'نتیجه'
        },
        'loading': 'در حال بارگذاری...',
        'noRecordsFound': 'هیچ داده‌ای در جدول وجود ندارد',  // ← این خط برای پیام خالی
        'error': 'خطا در بارگذاری داده‌ها'
    },
    pagination: {
        enabled: true,
        limit: 25,
        summary: true
    },
    search: {
        enabled: true,
        placeholder: 'جستجو...'
    },
    sort: true,
    resizable: true,
    className: {
        table: 'table table-bordered table-hover gridjs-table',
        thead: 'table-light gridjs-thead',
        th: 'gridjs-th',
        td: 'gridjs-td',
        container: 'gridjs-container',
        wrapper: 'gridjs-wrapper',
        footer: 'gridjs-footer',
        pagination: 'gridjs-pagination',
        summary: 'gridjs-summary',
        pages: 'gridjs-pages'
    }
});

// رندر کردن Grid.js در المان مورد نظر
gridInstance.render(gridContainer);