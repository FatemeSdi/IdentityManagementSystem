(function () {
    "use strict";

    /* funnel chart */
    var options = {
        series: [
            {
                name: "قیف",
                data: [1380, 1100, 990, 880, 740, 548, 330, 200],
            },
        ],
        chart: {
            type: 'bar',
            height: 350,
        },
        plotOptions: {
            bar: {
                borderRadius: 0,
                horizontal: true,
                barHeight: '80%',
                isFunnel: true,
            },
        },
        colors: [
            '#589bff',
        ],
        dataLabels: {
            enabled: true,
            formatter: function (val, opt) {
                return opt.w.globals.labels[opt.dataPointIndex] + ':  ' + val
            },
            dropShadow: {
                enabled: true,
            },
        },
        title: {
            text: 'قیف',
            align: 'middle',
        },
        xaxis: {
              categories: [
 "منبع",
 "غیره",
 "ارزیابی شده",
 "مصاحبه منابع انسانی",
 "فنی",
 "تأیید",
 "ارائه شده",
 "استخدام",
 ],
        },
        legend: {
            show: false,
        },
    };
    var chart = new ApexCharts(document.querySelector("#funnel-chart"), options);
    chart.render();
    /* funnel chart */

    /* pyramid chart */
    var options = {
        series: [
            {
                name: "",
                data: [200, 330, 548, 740, 880, 990, 1100, 1380],
            },
        ],
        chart: {
            type: 'bar',
            height: 350,
        },
        plotOptions: {
            bar: {
                borderRadius: 0,
                horizontal: true,
                distributed: true,
                barHeight: '80%',
                isFunnel: true,
            },
        },
        colors: [
            '#589bff', '#af6ded', '#fea45a', '#28d785', '#dd3a52', '#3a7adb', '#007c7c',
        ],
        dataLabels: {
            enabled: true,
            formatter: function (val, opt) {
                return opt.w.globals.labels[opt.dataPointIndex]
            },
            dropShadow: {
                enabled: true,
            },
        },
        title: {
            text: 'نمودار',
            align: 'middle',
        },
        xaxis: {
            categories: ['شیرینی ها', 'غذاهای فرآوری شده', 'چربی های سالم', 'گوشت', 'لوبیا و حبوبات', 'لبنیات', 'میوه ها و سبزیجات', 'غلات'],
        },
        legend: {
            show: false,
        },
    };

    var chart = new ApexCharts(document.querySelector("#pyramid-chart"), options);
    chart.render();
    /* pyramid chart */

})();