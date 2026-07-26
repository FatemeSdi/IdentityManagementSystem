(function () {
    "use strict"

  // Sample Data
const productsData = [
    ['SPK001', 'آریا رضایی', './assets/images/faces/2.jpg', 'مسدود', '0912-111-1111', '22 اسفند 1403', 'ایران'],
    ['SPK002', 'محمد حسینی', './assets/images/faces/3.jpg', 'فعال', '0935-111-1111', '20 اسفند 1403', 'ایران'],
    ['SPK003', 'مهدی کریمی', './assets/images/faces/4.jpg', 'مسدود', '0903-111-1111', '15 اسفند 1403', 'ایران'],
    ['SPK004', 'سارا احمدی', './assets/images/faces/7.jpg', 'مسدود', '0910-111-1111', '12 اسفند 1403', 'ایران'],
    ['SPK005', 'نرگس محمدی', './assets/images/faces/1.jpg', 'مسدود', '0922-111-1111', '9 اسفند 1403', 'ایران'],
    ['SPK006', 'علی مرادی', './assets/images/faces/7.jpg', 'فعال', '0938-111-1111', '6 اسفند 1403', 'ایران'],
    ['SPK007', 'رضا صادقی', './assets/images/faces/8.jpg', 'مسدود', '0919-111-1111', '1 اسفند 1403', 'ایران'],
    ['SPK008', 'فاطمه عباسی', './assets/images/faces/1.jpg', 'مسدود', '0990-111-1111', '29 بهمن 1403', 'ایران'],
];

const grid = new gridjs.Grid({
    columns: [
        {
            name: '#',
            formatter: (_, row) => gridjs.html(
                `<input class="form-check-input" type="checkbox" id="product-${row.cells[0].data}" value="" aria-label="...">`
            )
        },
        {
            name: 'کد',
            formatter: (_, row) => gridjs.html(
                `<a href="javascript:void(0);">${row.cells[0].data}</a>`
            )
        },
        {
            name: 'نام مشتری',
            formatter: (_, row) => gridjs.html(
                `<div class="d-flex align-items-center gap-3 position-relative">
                    <a href="product-details.php" class="stretched-link"></a>
                    <div class="lh-1">
                        <span class="avatar avatar-md avatar-rounded p-1 bg-light">
                            <img src="${row.cells[2].data}" alt="Product Image">
                        </span>
                    </div>
                    <div>
                        <span class="d-block fw-semibold">${row.cells[1].data}</span>
                        <span class="text-muted fs-13">${row.cells[6].data}</span>
                    </div>
                </div>`
            )
        },
        {
            name: 'وضعیت',
            formatter: (_, row) => gridjs.html(
                `<span class="badge bg-${row.cells[3].data === 'Active' ? 'secondary' : 'danger'}-transparent">${row.cells[3].data}</span>`
            )
        },
        {
            name: 'تلفن',
            formatter: (_, row) => gridjs.html(
                `${row.cells[4].data}`
            )
        },
        'تاریخ',
        {
            name: 'عملیات',
            formatter: (_, row) => gridjs.html(`
                <div class="dropdown">
                    <a href="javascript:void(0);" class="btn btn-icon btn-sm btn-primary-light" data-bs-toggle="dropdown" aria-expanded="false">
                        <i class="fe fe-more-vertical"></i>
                    </a>
                    <ul class="dropdown-menu">
                        <li><a class="dropdown-item" href="javascript:void(0);"><i class="ri-eye-line me-2"></i>نمایش</a></li>
                        <li><a class="dropdown-item" href="javascript:void(0);"><i class="ri-pencil-line me-2"></i>ویرایش</a></li>
                        <li><a class="dropdown-item" href="javascript:void(0);"><i class="ri-delete-bin-line me-2"></i>حذف</a></li>
                    </ul>
                </div>
            `)
        }
    ],
    data: productsData,
    pagination: true,
    search: false,
    sort: true
}).render(document.getElementById('product-table'));

// Filter functionality: event listeners for input and filter dropdowns
document.getElementById('search-input').addEventListener('input', (e) => applyFilters());
document.getElementById('status-filter').addEventListener('change', (e) => applyFilters());
document.getElementById('sort-filter').addEventListener('change', (e) => applyFilters());

// Function to apply search and filter logic
function applyFilters() {
    const searchInput = document.getElementById('search-input').value.toLowerCase();
    const statusFilter = document.getElementById('status-filter').value;
    const sortFilter = document.getElementById('sort-filter').value;

    const filteredData = productsData.filter(row => {
        const productName = row[1].toLowerCase();
        const status = row[3].toLowerCase();  // Corrected column index for 'Status'
        const searchCondition = productName.includes(searchInput);

        let statusCondition = true;
        // Apply the status filter if it's not 'all'
        if (statusFilter && statusFilter !== 'all') {
            statusCondition = status === statusFilter.toLowerCase();
        }

        return searchCondition && statusCondition;
    });

    if (sortFilter) {
        if (sortFilter === 'date') {
            filteredData.sort((a, b) => new Date(b[5]) - new Date(a[5])); // Corrected index for 'Joining Date'
        } else if (sortFilter === 'status') {
            filteredData.sort((a, b) => a[3].localeCompare(b[3])); // Sorting by Status
        } else if (sortFilter === 'name') {
            filteredData.sort((a, b) => a[1].localeCompare(b[1])); // Sorting by Customer Name
        }
    }

    grid.updateConfig({
        data: filteredData
    }).forceRender();

    // Handle the display of the "No matches found" row
    const gridContainer = document.getElementById('product-table');
    const tableBody = gridContainer.querySelector('.gridjs-tbody');
    
    // Clear previous "No matches found" row
    const noMatchesRow = document.getElementById('no-matches-row');
    if (noMatchesRow) {
        noMatchesRow.remove();
    }

    // If no results after filtering, create and append a "No matches found" row
    if (filteredData.length === 0) {
        const tr = document.createElement('tr');
        tr.id = 'no-matches-row';

        // Create a single cell spanning all columns
        const td = document.createElement('td');
        td.colSpan = 7; // Adjust the colspan to match the number of columns
        td.style.textAlign = 'center';
        td.textContent = 'No matching records found';
        td.style.fontWeight = '500';
        td.style.color = 'var(--default-text-color)';
        td.style.padding = '12px';

        tr.appendChild(td);
        tableBody.appendChild(tr);
    }
}


})();