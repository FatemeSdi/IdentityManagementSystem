(function () {
    "use strict"

    // Sample Data
    const productsData = [
        ['PRD54821', 'کیف زنانه چرم طبیعی', './assets/images/ecommerce/png/14.png', '۶۹,۹۹۰,۰۰۰ تومان', 'موجود', 'منتشر شده', '۱۲۰', '۲۲ اسفند ۱۴۰۳', 'الکترونیک'],
        ['PRD76439', 'هدست بی‌سیم بلوتوثی', './assets/images/ecommerce/png/16.png', '۸,۹۹۰,۰۰۰ تومان', 'ناموجود', 'پیش‌نویس', '۰', '۲۰ اسفند ۱۴۰۳', 'مد و پوشاک'],
        ['PRD19357', 'کفش مردانه اسپرت', './assets/images/ecommerce/png/15.png', '۳۹,۹۹۰,۰۰۰ تومان', 'موجود', 'منتشر شده', '۴۵', '۱۵ اسفند ۱۴۰۳', 'خانه و آشپزخانه'],
        ['PRD88214', 'گیاه آپارتمانی پتوس', './assets/images/ecommerce/png/17.png', '۱۲,۹۹۰,۰۰۰ تومان', 'موجود', 'منتشر شده', '۲۵۰', '۱۲ اسفند ۱۴۰۳', 'الکترونیک'],
        ['PRD45762', 'کیف زنانه صورتی', './assets/images/ecommerce/png/19.png', '۱۹,۹۹۰,۰۰۰ تومان', 'موجود', 'بایگانی شده', '۷۵', '۹ اسفند ۱۴۰۳', 'مد و پوشاک'],
        ['PRD60931', 'هدفون سامسونگ', './assets/images/ecommerce/png/11.png', '۱۴,۹۹۰,۰۰۰ تومان', 'ناموجود', 'پیش‌نویس', '۰', '۶ اسفند ۱۴۰۳', 'خانه و آشپزخانه'],
        ['PRD72548', 'کیف پشمی زنانه', './assets/images/ecommerce/png/12.png', '۷,۹۹۰,۰۰۰ تومان', 'موجود', 'منتشر شده', '۳۰۰', '۱ اسفند ۱۴۰۳', 'الکترونیک'],
        ['PRD33196', 'ساعت زنگ‌دار رومیزی', './assets/images/ecommerce/png/18.png', '۵,۹۹۰,۰۰۰ تومان', 'موجود', 'منتشر شده', '۱۵۰', '۲۹ بهمن ۱۴۰۳', 'مد و پوشاک'],
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
                    `<a href="javascript:void(0);">${row.cells[0].data}</a>`  // Correctly map to Product ID (row[0])
                )
            },
            {
                name: 'نام محصول',
                formatter: (_, row) => gridjs.html(
                    `<div class="d-flex align-items-center gap-3 position-relative">
                        <a href="product-details.php" class="stretched-link"></a>
                        <div class="lh-1">
                            <span class="avatar avatar-md avatar-square bg-light">
                                <img src="${row.cells[2].data}" alt="Product Image">
                            </span>
                        </div>
                        <div>
                            <span class="d-block fw-semibold">${row.cells[1].data}</span>
                            <span class="text-muted fs-13">${row.cells[8].data}</span>
                        </div>
                    </div>`
                )
            },
            'قیمت',
            {
                name: 'وضعیت موجودی',
                formatter: (_, row) => gridjs.html(
                    `<span class="badge bg-${row.cells[4].data === 'In Stock' ? 'secondary' : 'danger'}-transparent">${row.cells[4].data}</span>`
                )
            },
            {
                name: 'تعداد',
                formatter: (_, row) => gridjs.html(
                    `${row.cells[6].data}` // Correctly map to Quantity (row[6])
                )
            },
            {
                name: 'وضعیت',
                formatter: (_, row) => gridjs.html(
                    `<span class="text-${row.cells[5].data === 'Published' ? 'primary' : row.cells[5].data === 'Archived' ? 'secondary' : 'danger'}">${row.cells[5].data}</span>`
                )
            },
            'تاریخ افزودن',
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
    document.getElementById('category-filter').addEventListener('change', (e) => applyFilters());
    document.getElementById('status-filter').addEventListener('change', (e) => applyFilters());
    document.getElementById('stock-filter').addEventListener('change', (e) => applyFilters());
    document.getElementById('sort-filter').addEventListener('change', (e) => applyFilters());

    // Function to apply search and filter logic
    function applyFilters() {
        const searchInput = document.getElementById('search-input').value.toLowerCase();
        const categoryFilter = document.getElementById('category-filter').value;
        const statusFilter = document.getElementById('status-filter').value;
        const stockFilter = document.getElementById('stock-filter').value;
        const sortFilter = document.getElementById('sort-filter').value;

        const filteredData = productsData.filter(row => {
            const productName = row[1].toLowerCase();
            const category = row[8].toLowerCase();
            const status = row[5].toLowerCase();
            const stock = row[4].toLowerCase();

            let formattedStock = "";
            if (row[4] === "In Stock") {
                formattedStock = "in-stock";
            } else if (row[4] === "Out of Stock") {
                formattedStock = "out-of-stock";
            }
            else if (row[4] === "Out of Stock") {
                formattedStock = "out-of-stock";
            }

            const searchCondition = productName.includes(searchInput);
            const categoryCondition = categoryFilter === '' || categoryFilter === 'all' || category === categoryFilter;
            const statusCondition = statusFilter === '' || statusFilter === 'all' || status === statusFilter;
            const stockCondition = stockFilter === '' || stockFilter === 'all' || formattedStock === stockFilter;

            return searchCondition && categoryCondition && statusCondition && stockCondition;
        });

        if (sortFilter) {
            if (sortFilter === 'date') {
                filteredData.sort((a, b) => new Date(b[7]) - new Date(a[7]));
            } else if (sortFilter === 'price') {
                filteredData.sort((a, b) => parseFloat(b[3].replace('$', '')) - parseFloat(a[3].replace('$', '')));
            } else if (sortFilter === 'name') {
                filteredData.sort((a, b) => a[1].localeCompare(b[1]));
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
              td.colSpan = 9; // Adjust the colspan to match the number of columns
              td.style.textAlign = 'center';
              td.textContent = 'هیچ رکورد منطبقی یافت نشد';
              td.style.fontWeight = '500';
              td.style.color = 'var(--default-text-color)';
              td.style.padding = '12px';
  
              tr.appendChild(td);
              tableBody.appendChild(tr);
          }
    }

})();