/* =====================================================================
   توست یکپارچه‌ی سامانه (Toastify با ظاهر yzen) — جایگزین toastr و alert.
   API ها:
     imsToast(type, message[, title])      type: success | error | warning | info
     showToster(type, title, message)       (هم‌امضا با scripts.js مشترک TaskTrack)
     showToast(message, isSuccess|type)     (امضای قدیمی صفحات لاگین/فراموشی رمز)
     toastr.success/error/warning/info(msg) (shim برای کدهای قدیمی)
   ===================================================================== */
(function () {
    'use strict';

    var TYPES = {
        success: { icon: 'ti ti-circle-check', title: 'موفق' },
        error: { icon: 'ti ti-alert-circle', title: 'خطا' },
        warning: { icon: 'ti ti-alert-triangle', title: 'هشدار' },
        info: { icon: 'ti ti-info-circle', title: 'اطلاع' }
    };

    function imsToast(type, message, title) {
        type = TYPES[type] ? type : 'info';
        if (message == null || String(message).trim() === '') return;
        var cfg = TYPES[type];

        // با DOM ساخته می‌شه (نه innerHTML) تا متن پیام‌های سرور امن نمایش داده بشه
        var node = document.createElement('div');
        node.className = 'ims-toast-inner';

        var icon = document.createElement('span');
        icon.className = 'ims-toast-icon';
        icon.innerHTML = '<i class="' + cfg.icon + '"></i>';

        var body = document.createElement('div');
        body.className = 'ims-toast-body';
        var t = document.createElement('div');
        t.className = 'ims-toast-title';
        t.textContent = title || cfg.title;
        var m = document.createElement('div');
        m.className = 'ims-toast-msg';
        m.textContent = String(message);
        body.appendChild(t);
        body.appendChild(m);

        node.appendChild(icon);
        node.appendChild(body);

        if (typeof Toastify === 'undefined') {
            window.console && console.warn('[imsToast]', type, message);
            return;
        }

        Toastify({
            node: node,
            duration: type === 'error' ? 6000 : 4000,
            close: true,
            gravity: 'top',
            position: 'left',
            stopOnFocus: true,
            className: 'ims-toast ims-toast-' + type,
            style: { background: 'none' }
        }).showToast();
    }

    window.imsToast = imsToast;

    window.showToster = function (type, title, message) {
        if (message === undefined) { message = title; title = null; }
        imsToast(type, message, title && title !== message && title !== 'پیام سیستم' ? title : null);
    };

    window.showToast = function (message, typeOrSuccess) {
        var type = typeof typeOrSuccess === 'boolean'
            ? (typeOrSuccess ? 'success' : 'error')
            : (typeOrSuccess || 'info');
        imsToast(type, message);
    };

    window.toastr = {
        options: {},
        success: function (m, t) { imsToast('success', m, t); },
        error: function (m, t) { imsToast('error', m, t); },
        warning: function (m, t) { imsToast('warning', m, t); },
        info: function (m, t) { imsToast('info', m, t); },
        clear: function () { },
        remove: function () { }
    };
})();
