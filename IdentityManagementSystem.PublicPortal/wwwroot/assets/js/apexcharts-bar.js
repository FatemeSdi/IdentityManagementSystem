(function () {
    "use strict";

    /* basic bar chart */
    var options = {
        series: [{
            data: [400, 430, 448, 470, 540, 580, 690, 1100, 1200, 1380]
        }],
        chart: {
            type: 'bar',
            height: 320
        },
        plotOptions: {
            bar: {
                borderRadius: 4,
                horizontal: true,
            }
        },
        colors: ["#589bff"],
        grid: {
            borderColor: '#f2f5f7',
        },
        dataLabels: {
            enabled: false
        },
        xaxis: {
            categories: ['کره جنوبی', 'کانادا', 'بریتانیا', 'هلند', 'ایتالیا', 'فرانسه', 'ژاپن',
 'ایالات متحده', 'چین', 'آلمان'
 ],
            labels: {
                show: true,
                style: {
                    colors: "#8c9097",
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-xaxis-label',
                },
            }
        },
        yaxis: {
            labels: {
                show: true,
                style: {
                    colors: "#8c9097",
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-yaxis-label',
                },
            }
        }
    };
    var chart = new ApexCharts(document.querySelector("#bar-basic"), options);
    chart.render();


    /* stacked bar chart */
    var options = {
        series: [{
            name: 'محسن',
            data: [44, 55, 41, 37, 22, 43, 21]
        }, {
            name: 'علی',
            data: [53, 32, 33, 52, 13, 43, 32]
        }, {
            name: 'احسان',
            data: [12, 17, 11, 9, 15, 11, 20]
        }, {
            name: 'نقی',
            data: [9, 7, 5, 8, 6, 9, 4]
        }, {
            name: 'پویا',
            data: [25, 12, 19, 32, 25, 24, 10]
        }],
        chart: {
            type: 'bar',
            height: 320,
            stacked: true,
        },
        plotOptions: {
            bar: {
                horizontal: true,
            },
        },
        stroke: {
            width: 1,
            colors: ['#fff']
        },
        colors: ["#589bff", "#af6ded", "#fea45a", "#dd3a52", "#28d785"],
        grid: {
            borderColor: '#f2f5f7',
        },
        title: {
            text: 'فروش کتاب‌های داستانی',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
        xaxis: {
            categories: [1400, 1401, 1402, 1403, 1404, 1405, 1406],
            labels: {
                formatter: function (val) {
                    return val 
                },
                style: {
                    colors: "#8c9097",
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-xaxis-label',
                },
            }
        },
        yaxis: {
            title: {
                text: undefined
            },
            labels: {
                show: true,
                style: {
                    colors: "#8c9097",
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-yaxis-label',
                },
            }
        },
        tooltip: {
            y: {
                formatter: function (val) {
                    return val 
                }
            }
        },
        fill: {
            opacity: 1
        },
        legend: {
            position: 'top',
            horizontalAlign: 'left',
            offsetX: 40
        }
    };
    var chart = new ApexCharts(document.querySelector("#bar-stacked"), options);
    chart.render();

    /* 100% stacked bar chart */
    var options = {
        series: [{
            name: 'دوچرخه',
            data: [44, 55, 41, 37, 22, 43, 21]
        }, {
            name: 'بنز',
            data: [53, 32, 33, 52, 13, 43, 32]
        }, {
            name: 'پژو',
            data: [12, 17, 11, 9, 15, 11, 20]
        }, {
            name: 'پیکان',
            data: [9, 7, 5, 8, 6, 9, 4]
        }, {
            name: 'رانا',
            data: [25, 12, 19, 32, 25, 24, 10]
        }],
        chart: {
            type: 'bar',
            height: 320,
            stacked: true,
            stackType: '100%'
        },
        plotOptions: {
            bar: {
                horizontal: true,
            },
        },
        stroke: {
            width: 1,
            colors: ['#fff']
        },
        colors: ["#589bff", "#af6ded", "#fea45a", "#dd3a52", "#28d785"],
        grid: {
            borderColor: '#f2f5f7',
        },
        title: {
            text: 'نمودار',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
        xaxis: {
            categories: [1400, 1401, 1402, 1403, 1404, 1405, 1406],
            labels: {
                show: true,
                style: {
                    colors: "#8c9097",
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-xaxis-label',
                },
            }
        },
        yaxis: {
            labels: {
                show: true,
                style: {
                    colors: "#8c9097",
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-yaxis-label',
                },
            }
        },
        tooltip: {
            y: {
                formatter: function (val) {
                    return val 
                }
            }
        },
        fill: {
            opacity: 1

        },
        legend: {
            position: 'top',
            horizontalAlign: 'left',
            offsetX: 40
        }
    };
    var chart = new ApexCharts(document.querySelector("#bar-full"), options);
    chart.render();

   

    /* bar with markers */
    var options = {
        series: [
            {
                name: 'واقعی',
                data: [
                    {
                        x: '2011',
                        y: 12,
                        goals: [
                            {
                                name: 'مورد انتظار',
                                value: 14,
                                strokeWidth: 2,
                                strokeDashArray: 2,
                                strokeColor: '#775DD0'
                            }
                        ]
                    },
                    {
                        x: '2012',
                        y: 44,
                        goals: [
                            {
                                name: 'مورد انتظار',
                                value: 54,
                                strokeWidth: 5,
                                strokeHeight: 10,
                                strokeColor: '#775DD0'
                            }
                        ]
                    },
                    {
                        x: '2013',
                        y: 54,
                        goals: [
                            {
                                name: 'مورد انتظار',
                                value: 52,
                                strokeWidth: 10,
                                strokeHeight: 0,
                                strokeLineCap: 'round',
                                strokeColor: '#775DD0'
                            }
                        ]
                    },
                    {
                        x: '2014',
                        y: 66,
                        goals: [
                            {
                                name: 'مورد انتظار',
                                value: 61,
                                strokeWidth: 10,
                                strokeHeight: 0,
                                strokeLineCap: 'round',
                                strokeColor: '#775DD0'
                            }
                        ]
                    },
                    {
                        x: '2015',
                        y: 81,
                        goals: [
                            {
                                name: 'مورد انتظار',
                                value: 66,
                                strokeWidth: 10,
                                strokeHeight: 0,
                                strokeLineCap: 'round',
                                strokeColor: '#775DD0'
                            }
                        ]
                    },
                    {
                        x: '2016',
                        y: 67,
                        goals: [
                            {
                                name: 'مورد انتظار',
                                value: 70,
                                strokeWidth: 5,
                                strokeHeight: 10,
                                strokeColor: '#775DD0'
                            }
                        ]
                    }
                ]
            }
        ],
        chart: {
            height: 350,
            type: 'bar'
        },
        plotOptions: {
            bar: {
                horizontal: true,
            }
        },
        grid: {
            borderColor: '#f2f5f7',
        },
        colors: ['#af6ded'],
        dataLabels: {
            formatter: function (val, opt) {
                const goals =
                    opt.w.config.series[opt.seriesIndex].data[opt.dataPointIndex]
                        .goals

                if (goals && goals.length) {
                    return `${val} / ${goals[0].value}`
                }
                return val
            }
        },
        legend: {
            show: true,
            showForSingleSeries: true,
            customLegendItems: ['واقعی', 'مورد انتظار'],
            markers: {
                fillColors: ['#00E396', '#775DD0']
            }
        },
        xaxis: {
            labels: {
                show: true,
                style: {
                    colors: "#8c9097",
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-xaxis-label',
                },
            }
        },
        yaxis: {
            labels: {
                show: true,
                style: {
                    colors: "#8c9097",
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-yaxis-label',
                },
            }
        }
    };
    var chart = new ApexCharts(document.querySelector("#bar-markers"), options);
    chart.render();

    /* reversed bar chart */
    var options = {
        series: [{
            data: [400, 430, 448, 470, 540, 580, 690]
        }],
        chart: {
            type: 'bar',
            height: 320
        },
        annotations: {
            xaxis: [{
                x: 500,
                borderColor: '#00E396',
                label: {
                    borderColor: '#00E396',
                    style: {
                        color: '#fff',
                        background: '#00E396',
                    },
                    text: 'مقدار',
                }
            }],
            yaxis: [{
                y: 'July',
                y2: 'September',
                label: {
                    text: 'حاشیه نویسی'
                }
            }]
        },
        grid: {
            borderColor: '#f2f5f7',
        },
        colors: ["#589bff"],
        plotOptions: {
            bar: {
                horizontal: true,
            }
        },
        dataLabels: {
            enabled: true
        },
        xaxis: {
    		categories: ['فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور', 'مهر'],

            labels: {
                show: true,
                style: {
                    colors: "#8c9097",
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-xaxis-label',
                },
            }
        },
        grid: {
            xaxis: {
                lines: {
                    show: true
                }
            }
        },
        yaxis: {
            labels: {
                show: true,
                style: {
                    colors: "#8c9097",
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-yaxis-label',
                },
            },
            reversed: true,
            axisTicks: {
                show: true
            }
        }
    };
    var chart = new ApexCharts(document.querySelector("#bar-reversed"), options);
    chart.render();

    /* bar with categories as data labels */
    var options = {
        series: [{
            data: [400, 430, 448, 470, 540, 580, 690, 1100, 1200, 1380]
        }],
        chart: {
            type: 'bar',
            height: 320
        },
        plotOptions: {
            bar: {
                barHeight: '100%',
                distributed: true,
                horizontal: true,
                dataLabels: {
                    position: 'bottom'
                },
            }
        },
        colors: ["#589bff", "#af6ded", "#fea45a", "#dd3a52", "#28d785", "#fe7c58", "#007c7c", "#a65e9a",
            "#3a7adb", "#af6ded"
        ],
        grid: {
            borderColor: '#f2f5f7',
        },
        dataLabels: {
            enabled: true,
            textAnchor: 'center',
            style: {
                colors: ['#fff']
            },
            formatter: function (val, opt) {
                return opt.w.globals.labels[opt.dataPointIndex] + ":  " + val
            },
            offsetX: 5,
            dropShadow: {
                enabled: false
            }
        },
        stroke: {
            width: 1,
            colors: ['#fff']
        },
        xaxis: {
            categories: ['کره جنوبی', 'کانادا', 'بریتانیا', 'هلند', 'ایتالیا', 'فرانسه', 'ژاپن',
 'ایالات متحده', 'چین', 'هند'
 ],
            labels: {
                show: true,
                style: {
                    colors: "#8c9097",
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-xaxis-label',
                },
            }
        },
        yaxis: {
            labels: {
                show: false
            }
        },
        title: {
            text: 'اطلاعات',
            align: 'center',
            floating: true,
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
        subtitle: {
            text: 'نام دسته ها به عنوان اطلاعات در داخل نوارها',
            align: 'center',
        },
        tooltip: {
            theme: 'dark',
            x: {
                show: false
            },
            y: {
                title: {
                    formatter: function () {
                        return ''
                    }
                }
            }
        }
    };
    var chart = new ApexCharts(document.querySelector("#bar-categories"), options);
    chart.render();

    /* patterned bar chart */
    var options = {
        series: [{
            name: 'دوچرخه',
            data: [44, 55, 41, 37, 22, 43, 21]
        }, {
            name: 'بنز',
            data: [53, 32, 33, 52, 13, 43, 32]
        }, {
            name: 'پژو',
            data: [12, 17, 11, 9, 15, 11, 20]
        }, {
            name: 'پیکان',
            data: [9, 7, 5, 8, 6, 9, 4]
        }],
        chart: {
            type: 'bar',
            height: 350,
            stacked: true,
            dropShadow: {
                enabled: true,
                blur: 1,
                opacity: 0.25
            }
        },
        plotOptions: {
            bar: {
                horizontal: true,
                barHeight: '60%',
            },
        },
        dataLabels: {
            enabled: false
        },
        stroke: {
            width: 2,
        },
        colors: ["#589bff", "#af6ded", "#fea45a", "#28d785"],
        title: {
            text: 'مقایسه استراتژی فروش',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
        xaxis: {
            categories: [1400, 1401, 1402, 1403, 1404, 1405, 1406],
            labels: {
                show: true,
                style: {
                    colors: "#8c9097",
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-xaxis-label',
                },
            }
        },
        yaxis: {
            title: {
                text: undefined
            },
            labels: {
                show: true,
                style: {
                    colors: "#8c9097",
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-yaxis-label',
                },
            }
        },
        tooltip: {
            shared: false,
            y: {
                formatter: function (val) {
                    return val 
                }
            }
        },
        fill: {
            type: 'pattern',
            opacity: 1,
            pattern: {
                style: ['circles', 'slantedLines', 'verticalLines', 'horizontalLines'], // string or array of strings

            }
        },
        states: {
            hover: {
                filter: 'none'
            }
        },
        legend: {
            position: 'right',
            offsetY: 40
        }
    };
    var chart = new ApexCharts(document.querySelector("#bar-pattern"), options);
    chart.render();

    /* bar with image fill */
    var options = {
        series: [{
            name: 'سکه',
            data: [2, 4, 3, 4, 3, 5, 5, 6.5, 6, 5, 4, 5, 8, 7, 7, 8, 8, 10, 9, 9, 12, 12,
                11, 12, 13, 14, 16, 14, 15, 17, 19, 21
            ]
        }],
        chart: {
            type: 'bar',
            height: 350,
            animations: {
                enabled: false
            }
        },
        plotOptions: {
            bar: {
                horizontal: true,
                barHeight: '100%',
            },
        },
        dataLabels: {
            enabled: false,
        },
        stroke: {
            colors: ["#fff"],
            width: 0.2
        },
        labels: Array.apply(null, { length: 39 }).map(function (el, index) {
            return index + 1;
        }),
        xaxis: {
            labels: {
                show: true,
                style: {
                    colors: "#8c9097",
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-xaxis-label',
                },
            }
        },
        yaxis: {
            axisBorder: {
                show: false
            },
            axisTicks: {
                show: false
            },
            labels: {
                show: false
            },
            title: {
                text: 'وزن',
                style: {
                    color: "#8c9097",
                }
            },
        },
        grid: {
            position: 'back'
        },
        title: {
            text: 'مسیرهای پر شده با تصویر برش داده شده',
            align: 'right',
            offsetY: 25,
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
        fill: {
            type: 'image',
            opacity: 0.87,
            image: {
                src: ['assets/images/media/media-64.jpg'],
                width: 466,
                height: 406
            }
        },
    };
    var chart = new ApexCharts(document.querySelector("#bar-image"), options);
    chart.render();

})();