/*
 * Bridges the old Bootstrap 4 jQuery plugin API ($(...).modal(...), $(...).popover(...))
 * onto native Bootstrap 5 (window.bootstrap) for third-party plugins still written against
 * the old API (e.g. bootstrapPersianDatetimePicker) that we don't control the source of.
 *
 * bootstrap.bundle.min.js already defines $.fn.modal/$.fn.popover itself when jQuery is
 * present, so $.fn.modal is left untouched here (its native jQuery interface works fine).
 * $.fn.popover is deliberately OVERRIDDEN (not guarded) below: Bootstrap 5's popover
 * TemplateFactory deletes the .popover-body element entirely when `content` resolves to a
 * falsy value (Bootstrap 4 just left it empty). Plugins that create the popover with
 * content:"" and fill the body in later via direct DOM manipulation need that element to
 * still exist, so this override gives it a harmless placeholder instead of "".
 */
(function ($) {
    if (!$ || !window.bootstrap) return;

    $.fn.popover = function (optionsOrAction) {
        return this.each(function () {
            if (optionsOrAction === 'show') {
                bootstrap.Popover.getOrCreateInstance(this).show();
            } else if (optionsOrAction === 'hide') {
                var toHide = bootstrap.Popover.getInstance(this);
                if (toHide) toHide.hide();
            } else if (optionsOrAction === 'toggle') {
                bootstrap.Popover.getOrCreateInstance(this).toggle();
            } else if (optionsOrAction === 'update') {
                var toUpdate = bootstrap.Popover.getInstance(this);
                if (toUpdate) toUpdate.update();
            } else if (optionsOrAction === 'dispose') {
                var toDispose = bootstrap.Popover.getInstance(this);
                if (toDispose) toDispose.dispose();
            } else {
                var existing = bootstrap.Popover.getInstance(this);
                if (existing) existing.dispose();
                var opts = typeof optionsOrAction === 'object' ? Object.assign({}, optionsOrAction) : {};
                if (opts.html && !opts.content) opts.content = '&nbsp;';
                new bootstrap.Popover(this, opts);
            }
        });
    };
})(window.jQuery);
