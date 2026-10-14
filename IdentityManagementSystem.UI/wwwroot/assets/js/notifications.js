// wwwroot/assets/js/notifications.js
$(document).ready(function () {
    const normalizePath = function (path) {
        const normalized = (path || '').toLowerCase().replace(/\/+$/, '');
        return normalized || '/';
    };
    const currentPath = normalizePath(window.location.pathname);
    const forceChangePasswordPaths = [
        '/security/account/userprofile',
        '/security/account/changepassword'
    ];

    if ($('.dropdown-notification').length === 0 ||
        currentPath.includes('/account/login') ||
        forceChangePasswordPaths.includes(currentPath)) {
        return;
    }

    // Retrieve the anti-forgery token
    const antiForgeryToken = $('input[name="__RequestVerificationToken"]').val();
    const $markAllButton = $('.js-mark-all-notifications-read');
    const $notificationFooter = $('.dropdown-notification .notification-footer');
    let isMarkingAllAsRead = false;

    function fetchNotifications() {
        $.ajax({
            url: '/Notification/GetNotifications',
            type: 'GET',
            dataType: 'json',
            success: function (response) {
                const isSuccess = response && (response.Success === true || response.success === true);

                if (isSuccess) {
                    updateNotificationDropdown(response);
                } else {
                    console.warn('Notification request did not return success.', response);
                    updateNotificationDropdown({ count: 0, notifications: [] });
                }
            },
            error: function (xhr, status, error) {
                if (xhr.status == 401)
                    window.redirectToLoginPage();
                else if (xhr.status == 403 && xhr.responseJSON && xhr.responseJSON.ForceChangePassword) {
                    const redirectUrl = xhr.responseJSON.RedirectUrl || '/Security/Account/UserProfile';
                    const redirectPath = normalizePath(new URL(redirectUrl, window.location.origin).pathname);
                    if (redirectPath !== currentPath) {
                        window.location.href = redirectUrl;
                    }
                }
                else {
                    console.warn('Failed to retrieve notifications.', {
                        statusCode: xhr.status,
                        status: status,
                        error: error,
                        response: xhr.responseText
                    });
                    updateNotificationDropdown({ count: 0, notifications: [] });
                }
            }
        });
    }

    function updateNotificationDropdown(data) {
        const $dropdown = $('.dropdown-notification .media-list');
        const $dropdownBadge = $('.dropdown-notification .badge');
        const $notifHint = $('.dashboard-notification-hint');
        const $title = $('.dropdown-notification .notification-title');
        const $bellIcon = $('.dropdown-notification .bx-bell');
        const $dashboardBadge = $('.dashboard-notification-count');

        // بررسی وجود data و تنظیم مقادیر پیش‌فرض
        const count = data && typeof data.count === 'number' ? data.count : 0;
        const notifications = data && Array.isArray(data.notifications) ? data.notifications : [];

        // تنظیم تعداد اعلان‌ها
        $dropdownBadge.text(count);
        $title.text(`${count} اعلان جدید`);
        $dashboardBadge.text(count);

        // مدیریت انیمیشن آیکون زنگ
        if (count > 0) {
            $bellIcon.addClass('bx-tada');
        } else {
            $bellIcon.removeClass('bx-tada');
        }

        // خالی کردن لیست اعلان‌ها
        $dropdown.empty();

        // بررسی تعداد اعلان‌ها
        if (count === 0) {
            $dropdown.append('<div class="text-center p-1">هیچ اعلان جدیدی وجود ندارد</div>');
            $dropdownBadge.hide();
            $dashboardBadge.hide();
            $notifHint.hide();
            $markAllButton.addClass('d-none');
            $notificationFooter.addClass('d-none');
        } else {
            // افزودن اعلان‌ها به dropdown
            $.each(notifications, function (index, notification) {
                // بررسی وجود فیلدهای مورد نیاز در notification
                const notifyTime = notification.NotifyDate || notification.notifyDate || 'نامشخص';
                const executionId = notification.CheckListExecutionID || notification.checkListExecutionID || '';
                const notifyText = notification.NotifyText || notification.notifyText || 'بدون متن';
                const notificationId = notification.NotificationID || notification.notificationID || '';

                const html = `
                <div class="d-flex align-items-center gap-2 notification-item p-2 border-bottom"
                     style="cursor:pointer;"
                     data-id="${notificationId}"
                     data-execution-id="${executionId}">
                    <span class="avatar avatar-sm avatar-rounded bg-danger-transparent flex-shrink-0">
                        <i class="ti ti-bell fs-14"></i>
                    </span>
                    <div class="flex-fill" style="min-width:0;">
                        <div class="fw-medium fs-13 text-truncate">${notifyText}</div>
                        <div class="fs-11 text-muted notification-text">${notifyTime}</div>
                    </div>
                </div>`;
                $dropdown.append(html);
            });
            $dropdownBadge.show();
            $dashboardBadge.show();
            $notifHint.show();
            $markAllButton.removeClass('d-none');
            $notificationFooter.removeClass('d-none');
        }
    }

    //  مدیریت کلیک روی نوتیفیکیشن
    $(document).on('click', '.notification-item', function () {
        const notifId = $(this).data('id');
        const executionId = $(this).data('execution-id');

        // علامت‌گذاری به عنوان خوانده شده
        $.ajax({
            url: '/Notification/MarkNotificationsAsRead',
            type: 'POST',
            data: {
                notifID: notifId,
                __RequestVerificationToken: antiForgeryToken
            },
            success: function () {
                window.location.href = '/Run/CheckListFilled/' + executionId;
            },
            error: function () {
                // حتی در صورت خطا هم هدایت شود
                window.location.href = '/Run/CheckListFilled/' + executionId;
            }
        });
    });

    function markAllAsRead() {
        if (isMarkingAllAsRead) {
            return;
        }

        isMarkingAllAsRead = true;
        $markAllButton.prop('disabled', true);

        $.ajax({
            url: '/Notification/MarkAllNotificationsAsRead',
            type: 'POST',
            data: { __RequestVerificationToken: antiForgeryToken },
            dataType: 'json',
            success: function (response) {
                if (response.Success) {
                    window.showToster('success', 'پیام سیستم', 'همه اعلان‌ها به عنوان خوانده شده علامت‌گذاری شدند');
                    fetchNotifications();
                } else {
                    window.showToster('error', 'پیام سیستم', response.Message);
                }
            },
            error: function (xhr, status, error) {
                if (xhr.status == 401) {
                    window.redirectToLoginPage();
                } else if (xhr.status == 400) {
                    window.showToster('error', 'پیام سیستم', 'خطای اعتبارسنجی توکن');
                    console.error('Anti-forgery token validation failed:', xhr.responseText);
                } else {
                    window.showToster('error', 'پیام سیستم', 'خطا در انجام عملیات');
                }
            },
            complete: function () {
                isMarkingAllAsRead = false;
                $markAllButton.prop('disabled', false);
            }
        });
    }

    fetchNotifications();
    setInterval(fetchNotifications, 30000);

    $markAllButton.on('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        markAllAsRead();
    });

    $('.dropdown-notification .dropdown-menu-footer a').on('click', function (e) {
        //باتن js همه اعلان ها
    });
});
