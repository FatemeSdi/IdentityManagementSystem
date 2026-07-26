(function () {
    "use strict"

    // Sample Data
    const ordersData = [
        ['#ORD54821', 'محمد رضایی', './assets/images/faces/9.jpg', '۶۹,۹۹۰,۰۰۰ تومان', 'در انتظار ارسال', 'در انتظار پرداخت', 'کارت بانکی (ویزا)', '۲۶ دی ۱۴۰۳، ۰۹:۴۵', 'm.rezaei@example.com'],
        ['#ORD76439', 'سارا احمدی', './assets/images/faces/1.jpg', '۸,۹۹۰,۰۰۰ تومان', 'ارسال شده', 'پرداخت شده', 'مسترکارت', '۱۴ بهمن ۱۴۰۳، ۱۴:۳۰', 's.ahmadi@example.com'],
        ['#ORD19357', 'علی کریمی', './assets/images/faces/10.jpg', '۳۹,۹۹۰,۰۰۰ تومان', 'تحویل شده', 'ناموفق', 'پی‌پال', '۲۰ اسفند ۱۴۰۳، ۱۱:۱۵', 'a.karimi@example.com'],
        ['#ORD88214', 'نرگس محمدی', './assets/images/faces/2.jpg', '۱۲,۹۹۰,۰۰۰ تومان', 'لغو شده', 'بازگشت وجه', 'اپل‌پی', '۱۶ فروردین ۱۴۰۴، ۱۶:۰۰', 'n.mohammadi@example.com'],
        ['#ORD45762', 'مهدی حسینی', './assets/images/faces/11.jpg', '۱۹,۹۹۰,۰۰۰ تومان', 'ارسال شده', 'لغو شده', 'پرداخت در محل', '۱۱ اردیبهشت ۱۴۰۴، ۱۰:۳۰', 'm.hosseini@example.com'],
        ['#ORD60931', 'فاطمه عباسی', './assets/images/faces/3.jpg', '۱۴,۹۹۰,۰۰۰ تومان', 'تحویل شده', 'بازگشت وجه', 'مسترکارت', '۲۰ خرداد ۱۴۰۴، ۱۵:۴۵', 'f.abbasi@example.com'],
        ['#ORD72548', 'رضا صادقی', './assets/images/faces/13.jpg', '۷,۹۹۰,۰۰۰ تومان', 'تحویل شده', 'پرداخت شده', 'پی‌پال', '۲۷ تیر ۱۴۰۴، ۱۳:۰۰', 'r.sadeghi@example.com'],
        ['#ORD33196', 'الهام موسوی', './assets/images/faces/4.jpg', '۵,۹۹۰,۰۰۰ تومان', 'در انتظار ارسال', 'در انتظار پرداخت', 'امریکن اکسپرس', '۳ شهریور ۱۴۰۴، ۱۲:۳۰', 'e.mousavi@example.com'],
        ['#ORD91827', 'حسین مرادی', './assets/images/faces/14.jpg', '۵,۹۹۰,۰۰۰ تومان', 'لغو شده', 'پرداخت شده', 'کارت بانکی (ویزا)', '۱۴ شهریور ۱۴۰۴، ۱۸:۰۰', 'h.moradi@example.com'],
        ['#ORD44073', 'زهرا کاظمی', './assets/images/faces/5.jpg', '۳,۹۹۰,۰۰۰ تومان', 'ارسال شده', 'ناموفق', 'پرداخت در محل', '۲۰ مهر ۱۴۰۴، ۰۸:۱۵', 'z.kazemi@example.com']
    ];


    const grid = new gridjs.Grid({
        columns: [
            {
                name: '#',
                formatter: (_, row) => gridjs.html(
                    `<input class="form-check-input" type="checkbox" id="order-${row.cells[0].data}" value="" aria-label="...">`
                )
            },
            {
                name: 'شماره سفارش',
                formatter: (_, row) => gridjs.html(
                    `<a href="javascript:void(0);" class="text-primary text-decoration-underline">${row.cells[0].data}</a>`  // Correctly map to Order ID (row[0])
                )
            },
            {
                name: 'مشتری',
                formatter: (_, row) => gridjs.html(
                    `<a href="order-details.php">
                        <div class="d-flex align-items-center gap-3 position-relative">
                            <div class="lh-1">
                                <span class="avatar avatar-md avatar-rounded">
                                    <img src="${row.cells[2].data}" alt="User Image">
                                </span>
                            </div>
                            <div>
                                <span class="d-block fw-semibold">${row.cells[1].data}</span>
                                <span class="text-muted fs-13">${row.cells[8].data}</span>
                            </div>
                        </div>
                    </a>`
                )
            },
            'قیمت',
            {
                name: 'وضعیت ارسال',
                formatter: (_, row) => gridjs.html(
                    `<span class="badge bg-${row.cells[4].data === 'Pending' ? 'warning' : row.cells[4].data === 'Shipped' ? 'info' : row.cells[4].data === 'Delivered' ? 'primary' : 'danger'}-transparent">${row.cells[4].data}</span>`
                )
            },
            {
                name: 'روش پرداخت',
                formatter: (_, row) => gridjs.html(
                    `${row.cells[6].data}`
                )
            },
            {
                name: 'وضعیت پرداخت',
                formatter: (_, row) => gridjs.html(
                    `<span class="text-${row.cells[5].data === 'Pending' ? 'info' : row.cells[5].data === 'Completed' ? 'primary' : row.cells[5].data === 'Failed' ? 'orange' : row.cells[5].data === 'Refunded' ? 'warning' : 'danger'}">${row.cells[5].data}</span>`
                )
            },
            'تاریخ سفارش',
            {
                name: 'عملیات',
                formatter: (_, row) => gridjs.html(`
                    <div class="dropdown">
                        <a href="javascript:void(0);" class="btn btn-icon btn-sm btn-primary-light border" data-bs-toggle="dropdown" aria-expanded="false">
                            <i class="fe fe-more-vertical"></i>
                        </a>
                        <ul class="dropdown-menu">
                            <li><a class="dropdown-item" href="order-details.php"><i class="ri-eye-line me-2"></i>نمایش</a></li>
                            <li><a class="dropdown-item btn-delete" href="javascript:void(0);"><i class="ri-delete-bin-line me-2"></i>حذف</a></li>
                        </ul>
                    </div>
                `)
            }
        ],
        data: ordersData,
        pagination: true,
        search: false,
        sort: true
    }).render(document.getElementById('orders-table'));

    // Filter functionality: event listeners for input and filter dropdowns
    document.getElementById('search-input').addEventListener('input', (e) => applyFilters());
    document.getElementById('delivery-status-filter').addEventListener('change', (e) => applyFilters());
    document.getElementById('payment-status-filter').addEventListener('change', (e) => applyFilters());

    // Function to apply search and filter logic
    function applyFilters() {
        const searchInput = document.getElementById('search-input').value.toLowerCase();
        const paymentstatusFilter = document.getElementById('payment-status-filter').value;
        const deliverystatusFilter = document.getElementById('delivery-status-filter').value;

        const filteredData = ordersData.filter(row => {
            const customerName = row[1].toLowerCase();

            let deliveryStatus = "";
            if (row[4] === "Pending") {
                deliveryStatus = "pending";
            } else if (row[4] === "Shipped") {
                deliveryStatus = "shipped";
            } else if (row[4] === "Delivered") {
                deliveryStatus = "delivered";
            } else if (row[4] === "Cancelled") {
                deliveryStatus = "cancelled";
            }

            let paymentStatus = "";
            if (row[5] === "Pending") {
                paymentStatus = "pending";
            } else if (row[5] === "Completed") {
                paymentStatus = "completed";
            } else if (row[5] === "Failed") {
                paymentStatus = "failed";
            } else if (row[5] === "Refunded") {
                paymentStatus = "refunded";
            } else if (row[5] === "Cancelled") {
                paymentStatus = "cancelled";
            }

            const searchCondition = customerName.includes(searchInput);
            const paymentCondition = paymentstatusFilter === '' || paymentstatusFilter === 'all' || paymentStatus === paymentstatusFilter;
            const deliveryCondition = deliverystatusFilter === '' || deliverystatusFilter === 'all' || deliveryStatus === deliverystatusFilter;

            return searchCondition && paymentCondition && deliveryCondition;
        });

        grid.updateConfig({
            data: filteredData
        }).forceRender();

        // Handle the display of the "No matches found" row
        const gridContainer = document.getElementById('orders-table');
        const tableBody = gridContainer.querySelector('.gridjs-tbody');

        // Clear previous "No matches found" row
        const notFoundElement = document.querySelector('.gridjs-notfound');
        if (notFoundElement) {
            notFoundElement.style.display = 'none';  // Hide it using JavaScript
        }

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
            td.textContent = 'No matching records found';
            td.style.fontWeight = '500';
            td.style.color = 'var(--default-text-color)';
            td.style.padding = '12px';

            tr.appendChild(td);
            tableBody.appendChild(tr);
        }
    }

    // Add a listener for delete actions in the table with SweetAlert confirmation
    document.addEventListener('click', function (e) {
        if (e.target && e.target.classList.contains('btn-delete')) {
            Swal.fire({
                title: 'Are you sure?',
                text: "You won't be able to revert this!",
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, delete it!'
            }).then((result) => {
                if (result.isConfirmed) {
                    // Find the row index of the order to delete
                    const rowIndex = e.target.closest('tr').rowIndex - 1; // Subtract 1 to account for the header row

                    // Remove the order from the ordersData array
                    ordersData.splice(rowIndex, 1);

                    // Update the grid with the new data
                    grid.updateConfig({
                        data: ordersData
                    }).forceRender();

                    Swal.fire(
                        'Deleted!',
                        'Your order has been deleted.',
                        'success'
                    );
                }
            });
        }
    });

})();