(function () {
    'use script';

    // basic example
    new gridjs.Grid({
        columns: [{
            name: "تاریخ",
            width: "150px",
        }, {
            name: "نام",
            width: "150px",
        }, {
            name: "ایمیل",
            width: "200px",
        }, {
            name: "شناسه",
            width: "150px",
        }, {
            name: "قیمت",
            width: "100px",
        }, {
            name: "مقدار",
            width: "100px",
        }, {
            name: "مجموع",
            width: "100px",
        }],
        data: [
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
           ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
           ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"]
        ],
    }).render(document.getElementById("grid-example1"));
    // basic example

    // with pagination
    new gridjs.Grid({
        pagination: true,
        columns: [{
            name: "تاریخ",
            width: "150px",
        }, {
            name: "نام",
            width: "150px",
        }, {
            name: "ایمیل",
            width: "200px",
        }, {
            name: "شناسه",
            width: "150px",
        }, {
            name: "قیمت",
            width: "100px",
        }, {
            name: "مقدار",
            width: "100px",
        }, {
            name: "مجموع",
            width: "100px",
        }],
        data: [
           ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
           ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"]
        ],
    }).render(document.getElementById("grid-pagination"));;
    // with pagination

    // with search
    new gridjs.Grid({
        pagination: true,
        search: true,
        columns: [{
            name: "تاریخ",
            width: "150px",
        }, {
            name: "نام",
            width: "150px",
        }, {
            name: "ایمیل",
            width: "200px",
        }, {
            name: "شناسه",
            width: "150px",
        }, {
            name: "قیمت",
            width: "100px",
        }, {
            name: "مقدار",
            width: "100px",
        }, {
            name: "مجموع",
            width: "100px",
        }],
        data: [
           ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
           ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"]
        ],
    }).render(document.getElementById("grid-search"));;
    // with search

    // with sorting
    new gridjs.Grid({
        pagination: true,
        search: true,
        sort: true,
        columns: [{
            name: "تاریخ",
            width: "150px",
        }, {
            name: "نام",
            width: "150px",
        }, {
            name: "ایمیل",
            width: "200px",
        }, {
            name: "شناسه",
            width: "150px",
        }, {
            name: "قیمت",
            width: "100px",
        }, {
            name: "مقدار",
            width: "100px",
        }, {
            name: "مجموع",
            width: "100px",
        }],
        data: [
           ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
           ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"]
        ],
    }).render(document.getElementById("grid-sorting"));;
    // with sorting

    // loading state
    new gridjs.Grid({
        columns: [{
            name: "تاریخ",
            width: "150px",
        }, {
            name: "نام",
            width: "150px",
        }, {
            name: "ایمیل",
            width: "200px",
        }, {
            name: "شناسه",
            width: "150px",
        }, {
            name: "قیمت",
            width: "100px",
        }, {
            name: "مقدار",
            width: "100px",
        }, {
            name: "مجموع",
            width: "100px",
        }],
        pagination: true,
        search: true,
        sort: true,
        data: () => {
            return new Promise(resolve => {
                setTimeout(() =>
                    resolve([
                       ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
                       ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
                        ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
                        ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
                        ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"]
                    ]), 2000);
            });
        }
    }).render(document.getElementById("grid-loading"));
    // loading state

    //wide tables
    new gridjs.Grid({
        columns: [{
            name: "تاریخ",
            width: "150px",
        }, {
            name: "نام",
            width: "150px",
        }, {
            name: "ایمیل",
            width: "200px",
        }, {
            name: " شناسه سفارش",
            width: "150px",
        }, {
            name: "محصول",
            width: "150px",
        }, {
            name: "دسته بندی",
            width: "150px",
        }, {
            name: "قیمت",
            width: "100px",
        }, {
            name: "مقدار",
            width: "100px",
        }, {
            name: "مجموع",
            width: "100px",
        }],
        style: {
            table: {
                'white-space': 'nowrap'
            }
        },
        resizable: true,
        sort: true,
        pagination: true,
        data: [
            ["1403-10-15 12:50", "نازنین", "john123@gmail.com", "#12012", "ساعت هوشمند", "الکترونیک", "500 تومان", "1", "700 تومان"],
             ["1403-10-15 12:50", "سارا", "john123@gmail.com", "#12012", "ساعت هوشمند", "الکترونیک", "500 تومان", "1", "700 تومان"],
             ["1403-10-15 12:50", "پارسا", "john123@gmail.com", "#12012", "ساعت هوشمند", "الکترونیک", "500 تومان", "1", "700 تومان"],
             ["1403-10-15 12:50", "پویا", "john123@gmail.com", "#12012", "ساعت هوشمند", "الکترونیک", "500 تومان", "1", "700 تومان"],
             ["1403-10-15 12:50", "محمد", "john123@gmail.com", "#12012", "ساعت هوشمند", "الکترونیک", "500 تومان", "1", "700 تومان"]
        ],
    }).render(document.getElementById("grid-wide"));
    //wide tables

    // fixed header
    new gridjs.Grid({
        pagination: true,
        search: true,
        sort: true,
        fixedHeader: true,
        height: '350px',
        columns: [{
            name: "تاریخ",
            width: "150px",
        }, {
            name: "نام",
            width: "150px",
        }, {
            name: "ایمیل",
            width: "200px",
        }, {
            name: "شناسه",
            width: "150px",
        }, {
            name: "قیمت",
            width: "100px",
        }, {
            name: "مقدار",
            width: "100px",
        }, {
            name: "مجموع",
            width: "100px",
        }],
        data: [
           ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
           ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
           ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
           ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
           ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"]
        ],
    }).render(document.getElementById("grid-header-fixed"));
    // fixed header

    // hidden columns
    new gridjs.Grid({
        columns: [{
            name: "تاریخ",
            hidden: true,
        }, {
            name: "نام",
            width: "150px",
        }, {
            name: "ایمیل",
            width: "200px",
        }, {
            name: "شناسه",
            width: "150px",
        }, {
            name: "قیمت",
            width: "100px",
        }, {
            name: "مقدار",
            width: "100px",
        }, {
            name: "مجموع",
            width: "100px",
        }],
        sort: true,
        search: true,
        pagination: true,
        data: [
           ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
           ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"],
            ["1403-12-25 12:45", "محسن", "john123@gmail.com", "#12012", "50 تومان", "1", "100 تومان"]
        ],
    }).render(document.getElementById("grid-hidden-column"));;
    // hidden columns

})();