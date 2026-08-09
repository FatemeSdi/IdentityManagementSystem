(function () {
    'use strict';

    /* for basic sweet alert */
    document.getElementById('basic-alert').onclick = function () {
        Swal.fire('سلام، این یک پیام هشدار ساده است')
    };
    document.getElementById('alert-text').onclick = function () {
        Swal.fire(
            'اینترنت؟',
            'هنوز هم از آن استفاده می‌کنید؟',
            'question'
        )
    }
    document.getElementById('alert-footer').onclick = function () {
        Swal.fire({
            icon: 'error',
            title: 'اوپس...',
            text: 'مشکلی پیش آمده است!',
            footer: '<a href="javascript:void(0);">چرا این خطا برای من نمایش داده شده است؟</a>'
        })
    }
    document.getElementById('long-window').onclick = function () {
        Swal.fire({
            imageUrl: 'https://placeholder.pics/svg/300x1500',
            imageHeight: 1500,
            imageAlt: 'یک تصویر بلند'
        })
    }
    document.getElementById('alert-description').onclick = function () {
        Swal.fire({
            title: '<strong>نمونه <u>HTML</u></strong>',
            icon: 'info',
            html:
                'می‌توانید از <b>متن ضخیم</b>، ' +
                '<a href="https://sweetalert2.github.io/" target="blank">لینک‌ها</a> ' +
                'و سایر تگ‌های HTML استفاده کنید',
            showCloseButton: true,
            showCancelButton: true,
            focusConfirm: false,
            confirmButtonText:
                '<i class="fe fe-thumbs-up"></i> عالیه!',
            confirmButtonAriaLabel: 'عالی بود',
            cancelButtonText:
                '<i class="fe fe-thumbs-down"></i>',
            cancelButtonAriaLabel: 'خوشم نیامد'
        })
    }
    document.getElementById('three-buttons').onclick = function () {
        Swal.fire({
            title: 'آیا می‌خواهید تغییرات ذخیره شوند؟',
            showDenyButton: true,
            showCancelButton: true,
            confirmButtonText: 'ذخیره',
            denyButtonText: `ذخیره نشود`,
        }).then((result) => {
            if (result.isConfirmed) {
                Swal.fire('با موفقیت ذخیره شد!', '', 'success')
            } else if (result.isDenied) {
                Swal.fire('تغییرات ذخیره نشدند', '', 'info')
            }
        })
    }
    document.getElementById('alert-dialog').onclick = function () {
        Swal.fire({
            position: 'top-end',
            icon: 'success',
            title: 'کار شما با موفقیت ذخیره شد',
            showConfirmButton: false,
            timer: 1500
        })
    }
    document.getElementById('alert-confirm').onclick = function () {
        Swal.fire({
            title: 'آیا مطمئن هستید؟',
            text: "این عملیات قابل بازگشت نیست!",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'بله، حذف شود!'
        }).then((result) => {
            if (result.isConfirmed) {
                Swal.fire(
                    'حذف شد!',
                    'فایل شما با موفقیت حذف شد.',
                    'success'
                )
            }
        })
    }
    document.getElementById('alert-parameter').onclick = function () {
        const swalWithBootstrapButtons = Swal.mixin({
            customClass: {
                confirmButton: 'btn btn-success ms-2',
                cancelButton: 'btn btn-danger'
            },
            buttonsStyling: false
        })
    
        swalWithBootstrapButtons.fire({
            title: 'آیا از حذف این مورد مطمئن هستید؟',
            text: "این عملیات قابل بازگشت نیست!",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'بله، حذف شود!',
            cancelButtonText: 'خیر، انصراف',
            reverseButtons: true
        }).then((result) => {
            if (result.isConfirmed) {
                swalWithBootstrapButtons.fire(
                    'حذف شد!',
                    'فایل شما با موفقیت حذف شد.',
                    'success'
                )
            } else if (
                result.dismiss === Swal.DismissReason.cancel
            ) {
                swalWithBootstrapButtons.fire(
                    'لغو شد',
                    'فایل شما همچنان محفوظ است 🙂',
                    'error'
                )
            }
        })
    }
    document.getElementById('alert-image').onclick = function () {
        Swal.fire({
            title: 'عالی!',
            text: 'پنجره همراه با تصویر سفارشی.',
            imageUrl: './assets/images/media/media-59.jpg',
            imageWidth: 400,
            imageHeight: 200,
            imageAlt: 'تصویر سفارشی',
        })
    }
    document.getElementById('alert-custom-bg').onclick = function () {
        Swal.fire({
            title: 'عرض، فاصله داخلی، رنگ و پس‌زمینه سفارشی.',
            width: 600,
            padding: '3em',
            color: '#716add',
            background: 'url(./assets/images/media/media-19.jpg)',
            backdrop: `
              rgba(0,0,0,0.3)
              url(./assets/images/gif's/1.gif)
              left top
              no-repeat
            `
        })
    }
    document.getElementById('alert-auto-close').onclick = function () {
        let timerInterval
        Swal.fire({
            title: 'این پیام به صورت خودکار بسته می‌شود!',
            html: 'این پنجره تا <b></b> میلی‌ثانیه دیگر بسته خواهد شد.',
            timer: 2000,
            timerProgressBar: true,
            didOpen: () => {
                Swal.showLoading()
                const b = Swal.getHtmlContainer().querySelector('b')
                timerInterval = setInterval(() => {
                    b.textContent = Swal.getTimerLeft()
                }, 100)
            },
            willClose: () => {
                clearInterval(timerInterval)
            }
        }).then((result) => {
            if (result.dismiss === Swal.DismissReason.timer) {
                console.log('پیام به دلیل پایان زمان بسته شد')
            }
        })
    }
    document.getElementById('alert-ajax').onclick = function () {
        Swal.fire({
            title: 'نام کاربری Github خود را وارد کنید',
            input: 'text',
            inputAttributes: {
                autocapitalize: 'off'
            },
            showCancelButton: true,
            confirmButtonText: 'جستجو',
            showLoaderOnConfirm: true,
            preConfirm: (login) => {
                return fetch(`https://jsonplaceholder.typicode.com/posts`)
                    .then(response => {
                        if (!response.ok) {
                            throw new Error(response.statusText)
                        }
                        return response.json()
                    })
                    .catch(error => {
                        Swal.showValidationMessage(
                            `درخواست ناموفق بود: ${error}`
                        )
                    })
            },
            allowOutsideClick: () => !Swal.isLoading()
        }).then((result) => {
            if (result.isConfirmed) {
                Swal.fire({
                    title: `آواتار ${result.value.login}`,
                    imageUrl: result.value.avatar_url
                })
            }
        })
    }
    
    document.getElementById('success-message').onclick = function () {
        Swal.fire({
            html: 
            '<div class="mt-3"><span class="svg-primary"><svg xmlns="http://www.w3.org/2000/svg" enable-background="new 0 0 24 24" height="60px" viewBox="0 0 24 24" width="60px" fill="#1f1f1f"><g><rect fill="none" height="24" width="24"/></g><g><g><path d="M18.49,9.88l0.08-0.85l0.18-1.95l-1.9-0.43l-0.84-0.19l-0.44-0.74l-0.99-1.68L12.79,4.8L12,5.14L11.21,4.8 L9.42,4.03L8.43,5.71L7.99,6.45L7.15,6.64l-1.9,0.43l0.18,1.94l0.08,0.85l-0.56,0.65l-1.29,1.48l1.29,1.47l0.56,0.65l-0.08,0.85 l-0.18,1.96l1.9,0.43l0.84,0.19l0.44,0.74l0.99,1.67l1.78-0.77L12,18.85l0.79,0.34l1.78,0.77l0.99-1.68l0.44-0.74l0.84-0.19 l1.9-0.43l-0.18-1.95l-0.08-0.85l0.56-0.65l1.29-1.47l-1.29-1.47L18.49,9.88z M10.09,16.72l-3.8-3.81l1.48-1.48l2.32,2.33 l5.85-5.87l1.48,1.48L10.09,16.72z" opacity=".3"/><path d="M23,11.99l-2.44-2.79l0.34-3.69l-3.61-0.82L15.4,1.5L12,2.96L8.6,1.5L6.71,4.69L3.1,5.5L3.44,9.2L1,11.99l2.44,2.79 l-0.34,3.7l3.61,0.82L8.6,22.5l3.4-1.47l3.4,1.46l1.89-3.19l3.61-0.82l-0.34-3.69L23,11.99z M19.05,13.47l-0.56,0.65l0.08,0.85 l0.18,1.95l-1.9,0.43l-0.84,0.19l-0.44,0.74l-0.99,1.68l-1.78-0.77L12,18.85l-0.79,0.34l-1.78,0.77l-0.99-1.67l-0.44-0.74 l-0.84-0.19l-1.9-0.43l0.18-1.96l0.08-0.85l-0.56-0.65l-1.29-1.47l1.29-1.48l0.56-0.65L5.43,9.01L5.25,7.07l1.9-0.43l0.84-0.19 l0.44-0.74l0.99-1.68l1.78,0.77L12,5.14l0.79-0.34l1.78-0.77l0.99,1.68l0.44,0.74l0.84,0.19l1.9,0.43l-0.18,1.95l-0.08,0.85 l0.56,0.65l1.29,1.47L19.05,13.47z"/><polygon points="10.09,13.75 7.77,11.42 6.29,12.91 10.09,16.72 17.43,9.36 15.95,7.87"/></g></g></svg></span><div class="mt-2 pt-2 fs-15"><h5 class="fw-semibold">موفقیت‌آمیز !</h5><p class="text-muted fs-14 mx-4 mb-0">درخواست شما تایید شد.</p></div></div>',
            showCancelButton: true,
            showConfirmButton: false,
            customClass: { cancelButton: "btn btn-primary btn-sm mb-1" },
            cancelButtonText: "بازگشت",
            buttonsStyling: false,
            showCloseButton: true
        });
    }
    
    document.getElementById('error-message').onclick = function () {
        Swal.fire({
            html: 
            '<div class="mt-3"><span class="svg-danger"><svg xmlns="http://www.w3.org/2000/svg" height="60px" viewBox="0 0 24 24" width="60px" fill="#1f1f1f"><path d="M0 0h24v24H0V0z" fill="none"/><path d="M4 17.17L5.17 16H20V4H4v13.17zM11 6h2v4h-2V6zm0 6h2v2h-2v-2z" opacity=".3"/><path d="M20 2H4c-1.1 0-1.99.9-1.99 2L2 22l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm0 14H5.17L4 17.17V4h16v12zm-9-4h2v2h-2zm0-6h2v4h-2z"/></svg><h5 class="mt-3 fw-semibold">خطا !</h5><p class="text-muted fs-14 mx-4 mb-0">ایمیل شما نامعتبر است.</p></div>',
            showCancelButton: true,
            showConfirmButton: false,
            customClass: { cancelButton: "btn btn-danger btn-sm mb-1" },
            cancelButtonText: "تلاش مجدد", 
            buttonsStyling: false,
            showCloseButton: true
        });
    }
    
    document.getElementById('warning-message').onclick = function () {
        Swal.fire({
            html: 
            '<div class="mt-3"><span class="svg-warning"><svg xmlns="http://www.w3.org/2000/svg" height="60px" viewBox="0 0 24 24" width="60px" fill="#1f1f1f"><path d="M0 0h24v24H0V0z" fill="none"/><path d="M12 5.99L4.47 19h15.06L12 5.99zM13 18h-2v-2h2v2zm-2-4v-4h2v4h-2z" opacity=".3"/><path d="M12 2L1 21h22L12 2zm0 3.99L19.53 19H4.47L12 5.99zM11 16h2v2h-2zm0-6h2v4h-2z"/></svg><h5 class="mt-3 fw-semibold">حذف رسانه !</h5><p class="text-muted fs-14 mx-4 mb-0">آیا مطمئن هستید که می‌خواهید رسانه را حذف کنید؟</p></div>',
            showCancelButton: true,
            showConfirmButton: false,
            customClass: { cancelButton: "btn btn-warning btn-sm mb-1" },
            cancelButtonText: "حذف", 
            buttonsStyStyling: false,
            showCloseButton: true
        });
    }
    
    document.getElementById('email-message').onclick = function () {
        Swal.fire({
            html: 
            '<div class="mt-3"><span><span  class="avatar avatar-xl avatar-rounded bg-light svg-secondary"><svg xmlns="http://www.w3.org/2000/svg" height="60px" viewBox="0 0 24 24" width="60px" fill="#1f1f1f"><path d="M0 0h24v24H0V0z" fill="none"/><path d="M12 15.36l-8-5.02V18h16l-.01-7.63z" opacity=".3"/><path d="M21.99 8c0-.72-.37-1.35-.94-1.7L12 1 2.95 6.3C2.38 6.65 2 7.28 2 8v10c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2l-.01-10zM12 3.32L19.99 8v.01L12 13 4 8l8-4.68zM4 18v-7.66l8 5.02 7.99-4.99L20 18H4z"/></svg></span><h5 class="mt-3 fw-semibold">ایمیل خود را تایید کنید</h5><p class="text-muted fs-14 mx-4 mb-0">ایمیل ارسال شد<a href="javascript:void(0);" class="text-default text-decoration-underline">nehav123@gmail.com</a>لطفا چک کنید</p></div></div>',
            showCancelButton: true,
            showConfirmButton: false,
            customClass: { cancelButton: "btn btn-secondary btn-sm mb-1" },
            cancelButtonText: "تایید ایمیل", 
            buttonsStyling: false,
            showCloseButton: true,
            footer: '<p class="fs-14 text-muted mb-0">ایمیل را دریافت نکردید؟ <a href="javascript:void(0);" class="fw-semibold  text-danger text-decoration-underline">ارسال مجدد</a></p>',
        });
    }
    
    document.getElementById('notification-message').onclick = function () {
        Swal.fire({
            html: 
            '<div class="mt-3"><span class="svg-warning"><span  class="avatar avatar-xl avatar-rounded bg-light svg-info"><svg xmlns="http://www.w3.org/2000/svg" height="24px" viewBox="0 0 24 24" width="24px" fill="#1f1f1f"><path d="M0 0h24v24H0V0z" fill="none"/><path d="M12 6.5c-2.49 0-4 2.02-4 4.5v6h8v-6c0-2.48-1.51-4.5-4-4.5z" opacity=".3"/><path d="M12 22c1.1 0 2-.9 2-2h-4c0 1.1.9 2 2 2zm6-11c0-3.07-1.63-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.64 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2v-5zm-2 6H8v-6c0-2.48 1.51-4.5 4-4.5s4 2.02 4 4.5v6zM7.58 4.08L6.15 2.65C3.75 4.48 2.17 7.3 2.03 10.5h2c.15-2.65 1.51-4.97 3.55-6.42zm12.39 6.42h2c-.15-3.2-1.73-6.02-4.12-7.85l-1.42 1.43c2.02 1.45 3.39 3.77 3.54 6.42z"/></svg></span><h5 class="mt-3 fw-semibold">خوش آمدید Jock Mellar !</h5><p class="text-muted fs-14 mx-4 mb-0">شما <span class="text-primary fw-medium">32</span> اعلان دارید</p></div>',
            showCancelButton: true,
            showConfirmButton: false,
            customClass: { cancelButton: "btn btn-info btn-sm mb-1" },
            cancelButtonText: "مشاهده همه", 
            buttonsStyling: false,
            showCloseButton: true
        });
    }


})();