/*
NOTE:
------
PLACE HERE YOUR OWN JAVASCRIPT CODE IF NEEDED
WE WILL RELEASE FUTURE UPDATES SO IN ORDER TO NOT OVERWRITE YOUR JAVASCRIPT CODE PLEASE CONSIDER WRITING YOUR SCRIPT HERE.  */

var blockElement = function (element) {
    let block_ele = $(element);
    $(block_ele).block({
        message: '<div class="semibold"><span class="bx bx-loader-alt icon-spin "></span></div>',
        overlayCSS: {
            backgroundColor: "#ffffff",
            opacity: 0.8,
            cursor: 'wait'
        },
        css: {
            border: 0,
            padding: 0,
            backgroundColor: 'transparent',
            width: '100%',
        }
    });
};

var unblockElement = function (element) {
    let block_ele = $(element);
    $(block_ele).unblock();
};

var showToster = function (type, title, message) {
    var colors = {
        success: '#22c55e',
        info: '#3b82f6',
        error: '#ef4444',
        warning: '#f59e0b'
    };

    var text = (title && String(title).trim() && title !== message) ? (title + ': ' + message) : message;

    Toastify({
        text: text,
        duration: 3500,
        close: true,
        gravity: 'top',
        position: 'left',
        stopOnFocus: true,
        style: { background: colors[type] || colors.info }
    }).showToast();
}

var redirectToLoginPage = function () {
    window.location.href = "/Security/Account/Login?returnUrl=" + encodeURIComponent(window.location.pathname + window.location.search);
}
var redirectToAccessDeniedPage = function () {
    window.location.href = "/Error/AccessDenied";
}

var normalizePersianSearchText = function (value) {
    return (value || '')
        .toString()
        .trim()
        .replace(/ي/g, 'ی')
        .replace(/ى/g, 'ی')
        .replace(/ك/g, 'ک')
        .toLowerCase();
}

var findSearchSubmitButton = function (scope) {
    return scope
        .find('button:not(:disabled), a.btn:not(.disabled), input[type="button"]:not(:disabled), input[type="submit"]:not(:disabled)')
        .filter(function () {
            var button = $(this);
            var id = (button.attr('id') || '').toLowerCase();
            var name = (button.attr('name') || '').toLowerCase();
            var title = button.attr('title') || '';
            var text = button.is('input') ? (button.val() || '') : (button.text() || '');
            var normalizedTitle = normalizePersianSearchText(title);
            var normalizedText = normalizePersianSearchText(text);
            var isClearButton = id.indexOf('remove') >= 0
                || id.indexOf('clear') >= 0
                || name.indexOf('remove') >= 0
                || name.indexOf('clear') >= 0
                || normalizedTitle.indexOf('حذف') >= 0
                || normalizedTitle.indexOf('پاک') >= 0
                || normalizedText.indexOf('حذف') >= 0
                || normalizedText.indexOf('پاک') >= 0;

            if (isClearButton) {
                return false;
            }

            return /^search/i.test(button.attr('id') || '')
                || id.indexOf('search') >= 0
                || name.indexOf('search') >= 0
                || normalizedTitle.indexOf('جستجو') >= 0
                || normalizedText.indexOf('جستجو') >= 0;
        })
        .first();
};

$(document).on('keydown', 'input, select', function (e) {
    var key = e.key || (e.which === 13 ? 'Enter' : '');

    if (key !== 'Enter' || e.isDefaultPrevented() || e.isComposing) {
        return;
    }

    var field = $(this);

    if (field.closest('.dataTables_filter, .select2-search').length || field.is('[type="button"], [type="submit"], [type="reset"], [type="checkbox"], [type="radio"], [type="file"]')) {
        return;
    }

    var searchButton = $();
    var searchScopes = [
        field.closest('[data-search-scope], .simple-search'),
        field.closest('form'),
        field.closest('.collapse[id*="Search"], .collapse[id*="search"], #collapseSearch'),
        field.closest('.row'),
        field.closest('.card-body')
    ];

    $.each(searchScopes, function (_, scope) {
        if (searchButton.length || !scope.length) {
            return;
        }

        searchButton = findSearchSubmitButton(scope);
    });

    if (!searchButton.length) {
        return;
    }

    e.preventDefault();
    searchButton.trigger('click');
});

if ($.fn.dataTable && $.fn.dataTable.ext && $.fn.dataTable.ext.type && $.fn.dataTable.ext.type.search) {
    $.fn.dataTable.ext.type.search.string = normalizePersianSearchText;
    $.fn.dataTable.ext.type.search.html = function (value) {
        return normalizePersianSearchText($('<div>').html(value || '').text());
    };

    $(document).on('keyup input', '.dataTables_filter input[type="search"]', function () {
        var wrapper = $(this).closest('.dataTables_wrapper');
        var tableId = wrapper.attr('id') ? wrapper.attr('id').replace(/_wrapper$/, '') : '';
        var table = tableId ? $('#' + tableId) : $();

        if (table.length && $.fn.DataTable.isDataTable(table)) {
            table.DataTable().search(normalizePersianSearchText(this.value)).draw();
        }
    });
}

if ($.fn.select2 && $.fn.select2.defaults) {
    $.fn.select2.defaults.set('matcher', function (params, data) {
        var term = normalizePersianSearchText(params.term);
        var text = normalizePersianSearchText(data.text);

        if (!term || text.indexOf(term) > -1) {
            return data;
        }

        return null;
    });
}

var initialDataTable = function ({ element, url, columns, columnDefs = [], order = [[0, "desc"]], serverSide = true, drawCallbackFunc = null, selectable = false, selectReferenceId = null, getSeletedDataFunc }) {

    if (serverSide && selectable) {
        window.showToster('error', 'پیام سیستم', 'خاصیت serverSide و selectable همزمان نمی توند فعال باشد');
        return;
    }

    if (selectable && typeof getSeletedDataFunc !== 'function') {
        window.showToster('error', 'پیام سیستم', 'getSeletedDataFunc پیاده سازی نشده است');
        return;
    }

    let dataTable = $(element).on('processing.dt', function (e, settings, processing) {
        if (processing)
            window.blockElement(element + '_wrapper');
        else
            window.unblockElement(element + '_wrapper');
    }).DataTable({
        dom: serverSide ? `<"H"lr>t<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>`:
         `<"row"<"col-sm-4"lr><"col-sm-8 d-flex justify-content-sm-end justify-content-xs-center"f>>t<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>`,
        destroy: true,
        serverSide: serverSide,
        processing: false,
        //responsive: true,
        responsive: {
            details: {
                type: 'column',
                target: 'tr'
            }
        },
        bFilter: true,
        autoWidth: false,
        info: true,
        stateSave: false,
        lengthMenu: selectable ? [[5], [5]] : [[10, 20, 50], [10, 20, 50]],
        lengthChange: true,
        ajax: {
            url: url,
            //dataSrc: "",
            type: 'POST',
            dataType: 'json',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            error: function (xhr, error, thrown) {

                if (xhr.status == 401)
                    window.redirectToLoginPage();
                else
                    window.showToster('error', 'پیام سیستم', 'خطا در ارتباط با سرور');
            },
            data: function (data) {
                if (selectReferenceId != null) {
                    data.referenceId = selectReferenceId;
                }

                if (selectable) {
                    return;
                } else if (serverSide) {
                    data.draw = data.draw || 0;
                    data.start = data.start || 0;
                    data.length = data.length || 10;
                    data.searchValue = data.search.value || '';
                    data.sortColumn = data.columns[data.order[0].column].data || '';
                    data.sortDirection = data.order[0].dir || '';
                }

            },
        },
        columns: columns,
        columnDefs: columnDefs,
        order: order,
        drawCallback: function (settings) {
           
            if (settings.json && settings.json.Success == false && settings.json.Status == 403) { //خطایی که ممکنه سیستمی رخ دهد
                window.redirectToAccessDeniedPage();
            } else if (settings.json && settings.json.success == false) //recieve data with false success
                window.showToster('error', settings.json.message);
            else {
                if (drawCallbackFunc != null) drawCallbackFunc();
            }
        },
        initComplete: function (settings, json) {
            if (selectable) {
                let isAllSelected = true;
                var rows = dataTable.rows().nodes();
                $('input[type="checkbox"]', rows).each(function (i, e) {
                    if (e.checked == false) {
                        isAllSelected = false;
                        return false;
                    }
                });
                $("input.selectAllChk:checkbox", dataTable.nodes()).prop('checked', isAllSelected);
                getSeletedDataFunc(getSelectedData());
            }

            window.unblockElement(element + '_wrapper');
        },
        oLanguage: {
            "sEmptyTable": "هیچ اطلاعاتی وجود ندارد",
            "sProcessing": "درحال بارگذاری...",
            "sZeroRecords": "رکوردی پیدا نشد",
            "sLengthMenu": "نمایش _MENU_ &nbsp;رکورد",
            "sInfo": "نمایش _START_ تا _END_ از _TOTAL_ رکورد",
            "sInfoEmpty": "نمایش 0 تا 0 از 0 رکورد",
            "sInfoFiltered": "(فیلتر شده از مجموع _MAX_  رکورد)",
            "sSearch": "جستجو: ",
            "oPaginate": {
                "sFirst": ">>",
                "sLast": "<<",
                "sNext": ">",
                "sPrevious": "<"
            }
        }
    });

    if (selectable) {
        function getSelectedData() {
            let _selectedData = [];
            var rows = dataTable.rows().nodes();
            $('input[type="checkbox"]', rows).each(function (i, e) {
                if (e.checked == true) {
                    _selectedData.push(e.value);
                }
            });

            return { referenceId: selectReferenceId, data: _selectedData };
        }
        // Handle click on "Select all" control
        dataTable.on('click', 'thead tr input[type="checkbox"]', function (e) {
            // Check/uncheck all checkboxes in the table
            var rows = dataTable.rows().nodes();
            $('input[type="checkbox"]', rows).prop('checked', this.checked);

            getSeletedDataFunc(getSelectedData());
        });

        // Handle click on checkbox to set state of "Select all" control
        dataTable.on('change', 'tbody tr input[type="checkbox"]', function (e) {

            let isAllSelected = true;
            var rows = dataTable.rows().nodes();
            $('input[type="checkbox"]', rows).each(function (i, e) {
                if (e.checked == false) {
                    isAllSelected = false;
                    return false;
                }
            });
            $("input.selectAllChk:checkbox", dataTable.nodes()).prop('checked', isAllSelected);

            getSeletedDataFunc(getSelectedData());
        });

       
    }
    return dataTable;
}

var DeleteRecordDataTable = function ({ blockElement = null, url, recordId, title, successCallBackFunc = null }) {
    Swal.fire({
        title: `حذف ${title}`,
        text: 'آیا مطمئن هستید؟',
        type: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'بله، حذف شود!',
        cancelButtonText: 'انصراف',
        confirmButtonClass: 'btn btn-warning',
        cancelButtonClass: 'btn btn-danger ml-1',
        buttonsStyling: false,
    }).then(function (result) {
        if (result.value) {
            if (blockElement != null) window.blockElement(blockElement);
            $.ajax({
                url: url,
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                data: { 'Id': recordId },
                type: "Post",
                dataType: "Json",
                success: function (data) {
                    window.showToster(data.Success ? 'success' : 'error', 'پیام سیستم', data.Message);
                    if (blockElement != null) window.unblockElement(blockElement);
                    if (data.Success && successCallBackFunc != null) successCallBackFunc();
                },
                error: function (xhr, status, err) {
                    if (xhr.status == 401)
                        window.redirectToLoginPage();
                    else {
                        if (blockElement != null) window.unblockElement(blockElement);
                        window.showToster('error', 'پیام سیستم', 'خطا در حذف اطلاعات');
                    }
                }
            });
        }
    });
}

$(".select2").select2({
    // the following code is used to disable x-scrollbar when click in select input and
    // take 100% width in responsive also
    dropdownAutoWidth: true,
    width: '100%'
});

$('.select2').on('select2:select', function (e) {

    var selectedData = e.params.data;
    if (e.target.classList.contains('requierd')) {
        if (selectedData.id == null || selectedData.id == '') {
            e.target.classList.add('input-validation-error');
        } else {
            e.target.classList.remove('input-validation-error');
        }
    }
    //console.log('Selected option:', selectedData);
});

// تقویم شمسی (persian-datepicker): جایگزین .PDTP/MdPersianDateTimePicker قدیمی برای ورودی‌های تاریخ
// روی input بگذارید: class="form-control fc-datepicker" و در صورت نیاز data-enabletimepicker="true"
// data-format دلخواه (نشانه‌های YYYY/MM/DD HH:mm:ss) قالب پیش‌فرض را بازنویسی می‌کند؛
// مثلاً وقتی سرور ثانیه را قبول نمی‌کند از data-format="YYYY/MM/DD HH:mm" استفاده کنید.
// آیکن باز کردن: class="fc-datepicker-icon" data-target="{id ورودی}"
// آیکن پاک کردن: class="fc-datepicker-clear" data-target="{id ورودی}"
if (typeof $.fn.persianDatepicker !== "undefined") {
    $('.fc-datepicker').each(function () {
        var $input = $(this);
        var enableTime = $input.data('enabletimepicker') === true || $input.data('enabletimepicker') === 'true';
        var format = $input.data('format') || (enableTime ? 'YYYY/MM/DD HH:mm:ss' : 'YYYY/MM/DD');
        $input.persianDatepicker({
            format: format,
            autoClose: true,
            initialValue: false,
            timePicker: { enabled: enableTime, second: { enabled: enableTime && /ss/i.test(format) } }
        });
    });
}
$('.fc-datepicker-icon').on('click', function () {
    var $input = $('#' + $(this).data('target'));
    $input.focus();
    if ($input.data('persianDatepicker')) $input.data('persianDatepicker').show();
});
$('.fc-datepicker-clear').on('click', function () {
    $('#' + $(this).data('target')).val('');
});

// آکاردئون منوی کناری: هر بخش (دسته) با کلیک روی عنوانش باز/بسته می‌شود
// در بارگذاری اولیه فقط بخش «داشبورد» باز است.
(function initSideMenuAccordion() {
    var mainMenu = document.querySelector('.main-menu');
    if (!mainMenu) return;

    if (mainMenu.querySelector('.slide.has-sub')) return;

    var categories = Array.prototype.filter.call(mainMenu.children, function (el) {
        return el.classList.contains('slide__category');
    });

    categories.forEach(function (categoryEl) {
        var items = [];
        var sibling = categoryEl.nextElementSibling;
        while (sibling && !sibling.classList.contains('slide__category')) {
            items.push(sibling);
            sibling = sibling.nextElementSibling;
        }

        var categoryName = categoryEl.querySelector('.category-name');
        var isDashboard = categoryName && categoryName.textContent.trim() === 'داشبورد';
        var isOpen = isDashboard;

        var icon = document.createElement('i');
        icon.className = 'ri-arrow-down-s-line category-toggle-icon';
        categoryEl.appendChild(icon);
        categoryEl.setAttribute('role', 'button');
        categoryEl.setAttribute('tabindex', '0');

        function applyState() {
            items.forEach(function (item) {
                item.classList.toggle('menu-item-collapsed', !isOpen);
            });
            categoryEl.classList.toggle('category-collapsed', !isOpen);
        }
        applyState();

        function toggle() {
            isOpen = !isOpen;
            applyState();
        }
        categoryEl.addEventListener('click', toggle);
        categoryEl.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                toggle();
            }
        });
    });
})();






