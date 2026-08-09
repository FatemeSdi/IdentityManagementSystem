(function () {
    "use strict";

    const tour = new Shepherd.Tour({
        defaultStepOptions: {
            cancelIcon: {
                enabled: true
            },
            classes: 'class-1 class-2',
            scrollTo: { behavior: 'smooth', block: 'center' }
        },
        useModalOverlay: {
            enabled: true,
        }
    });

    tour.addStep({
        id: 'step-1',
        title: "به برنامه تور ما خوش آمدید",
        text: 'تجربه سفر خود را با مقاصد، فعالیت‌ها، و مکان‌های دست‌چین‌شده متناسب با ترجیحات خود تنظیم کنید',
        attachTo: {
            element: '#step-1',
            on: 'bottom',
        },
        buttons: [
            {
                text: 'بعدی',
                action: tour.next,
            },
        ],
    });

    tour.addStep({
        id: 'step-2',
        title: "یک مقصد را انتخاب کنید",
        text: 'مقصدی را انتخاب کنید که مطابق با علایق و ترجیحات گروه باشد.',
        attachTo: {
            element: '#step-2',
            on: 'bottom',
        },
        buttons: [
            {
                text: 'بعدی',
                action: tour.next,
            },
        ],
    });

    tour.addStep({
        id: 'Set a Budget',
        title: "کتاب حمل و نقل و اسکان",
        text: 'بودجه ای را برای پوشش حمل و نقل، اقامت، غذا و فعالیت ها تعیین کنید.',
        attachTo: {
            element: '#step-3',
            on: 'bottom',
        },
        buttons: [
            {
                text: 'بعدی',
                action: tour.next,
            },
        ],
    });

    tour.addStep({
        id: 'step-3',
        title: "کتاب حمل و نقل و اسکان",
        text: 'حمل و نقل امن به مقصد و از مقصد و رزرو اقامتگاه مناسب.',
        attachTo: {
            element: '#step-4',
            on: 'bottom',
        },
        buttons: [
            {
                text: 'بعدی',
                action: tour.next,
            },
        ],
    });

    tour.addStep({
        id: 'step-5',
        title: "برنامه ریزی فعالیت ها",
        text: 'فعالیت ها یا جاذبه های کلیدی را برای هر روز از تور مشخص کنید.',
        attachTo: {
            element: '#step-5',
            on: 'bottom',
        },
        buttons: [
            {
                text: 'بعدی',
                action: tour.next,
            },
        ],
    });

    tour.addStep({
        id: 'step-6',
        title: "ارتباط برقرار کنید و تایید کنید",
        text: 'برنامه سفر را با شرکت کنندگان به اشتراک بگذارید، رزرو را تأیید کنید و اطمینان حاصل کنید که همه برای تور آماده هستند.',
        attachTo: {
            element: '#step-6',
            on: 'bottom',
        },
        buttons: [
            {
                text: 'بعدی',
                action: tour.next,
            },
        ],
    });
    
    tour.addStep({
        id: 'step-7',
        title: "سفر خود را شروع کنید",
        text: 'برنامه سفر را با شرکت کنندگان به اشتراک بگذارید، رزرو را تأیید کنید و اطمینان حاصل کنید که همه برای تور آماده هستند.',
        attachTo: {
            element: '#step-7',
            on: 'bottom',
        },
        buttons: [
            {
                text: 'پایان',
                action: tour.next,
            },
        ],
    });

    tour.start();

})();