(function () {
    "use strict";

    /* basic range area chart */
    var options = {
        series: [
            {
                name: 'دما تهران',
                data: [
                    {
                        x: 'فروردین',
                        y: [-2, 4]
                    },
                    {
                        x: 'اردیبهشت',
                        y: [-1, 6]
                    },
                    {
                        x: 'خرداد',
                        y: [3, 10]
                    },
                    {
                        x: 'تیر',
                        y: [8, 16]
                    },
                    {
                        x: 'مرداد',
                        y: [13, 22]
                    },
                    {
                        x: 'شهریور',
                        y: [18, 26]
                    },
                    {
                        x: 'مهر',
                        y: [21, 29]
                    },
                    {
                        x: 'ابان',
                        y: [21, 28]
                    },
                    {
                        x: 'اذر',
                        y: [17, 24]
                    },
                    {
                        x: 'دی',
                        y: [11, 18]
                    },
                    {
                        x: 'بهمن',
                        y: [6, 12]
                    },
                    {
                        x: 'اسفند',
                        y: [1, 7]
                    }
                ]
            }
        ],
        chart: {
            height: 350,
            type: 'rangeArea'
        },
        stroke: {
            curve: 'straight'
        },
        title: {
            text: 'دما تهران'
        },
        colors: ["#589bff"],
        markers: {
            hover: {
                sizeOffset: 5
            }
        },
        dataLabels: {
            enabled: false
        },
        yaxis: {
            labels: {
                formatter: (val) => {
                    return val
                }
            }
        }
    };
    var chart = new ApexCharts(document.querySelector("#rangearea-basic"), options);
    chart.render();

    /* combo range area chart */
    var options = {
        series: [
            {
                type: 'rangeArea',
                name: 'تیم 2',

                data: [
                    {
                        x: 'فروردین',
                        y: [1100, 1900]
                    },
                    {
                        x: 'اردیبهشت',
                        y: [1200, 1800]
                    },
                    {
                        x: 'خرداد',
                        y: [900, 2900]
                    },
                    {
                        x: 'تیر',
                        y: [1400, 2700]
                    },
                    {
                        x: 'مرداد',
                        y: [2600, 3900]
                    },
                    {
                        x: 'شهریور',
                        y: [500, 1700]
                    },
                    {
                        x: 'مهر',
                        y: [1900, 2300]
                    },
                    {
                        x: 'ابان',
                        y: [1000, 1500]
                    }
                ]
            },

            {
                type: 'rangeArea',
                name: 'تیم 1',
                data: [
                    {
                        x: 'فروردین',
                        y: [3100, 3400]
                    },
                    {
                        x: 'اردیبهشت',
                        y: [4200, 5200]
                    },
                    {
                        x: 'خرداد',
                        y: [3900, 4900]
                    },
                    {
                        x: 'تیر',
                        y: [3400, 3900]
                    },
                    {
                        x: 'مرداد',
                        y: [5100, 5900]
                    },
                    {
                        x: 'شهریور',
                        y: [5400, 6700]
                    },
                    {
                        x: 'مهر',
                        y: [4300, 4600]
                    },
                    {
                        x: 'ابان',
                        y: [2100, 2900]
                    }
                ]
            },

            {
                type: 'line',
                name: 'تیم 3',
                data: [
                    {
                        x: 'فروردین',
                        y: 1500
                    },
                    {
                        x: 'اردیبهشت',
                        y: 1700
                    },
                    {
                        x: 'خرداد',
                        y: 1900
                    },
                    {
                        x: 'تیر',
                        y: 2200
                    },
                    {
                        x: 'مرداد',
                        y: 3000
                    },
                    {
                        x: 'شهریور',
                        y: 1000
                    },
                    {
                        x: 'مهر',
                        y: 2100
                    },
                    {
                        x: 'ابان',
                        y: 1200
                    },
                    {
                        x: 'اذر',
                        y: 1800
                    },
                    {
                        x: 'دی',
                        y: 2000
                    }
                ]
            },
            {
                type: 'line',
                name: 'تیم 4',
                data: [
                    {
                        x: 'فروردین',
                        y: 3300
                    },
                    {
                        x: 'اردیبهشت',
                        y: 4900
                    },
                    {
                        x: 'خرداد',
                        y: 4300
                    },
                    {
                        x: 'تیر',
                        y: 3700
                    },
                    {
                        x: 'مرداد',
                        y: 5500
                    },
                    {
                        x: 'شهریور',
                        y: 5900
                    },
                    {
                        x: 'مهر',
                        y: 4500
                    },
                    {
                        x: 'ابان',
                        y: 2400
                    },
                    {
                        x: 'اذر',
                        y: 2100
                    },
                    {
                        x: 'دی',
                        y: 1500
                    }
                ]
            }
        ],
        chart: {
            height: 350,
            type: 'rangeArea',
            animations: {
                speed: 500
            }
        },
        colors: ['#589bff', '#af6ded', '#589bff', '#af6ded'],
        dataLabels: {
            enabled: false
        },
        fill: {
            opacity: [0.24, 0.24, 1, 1]
        },
        forecastDataPoints: {
            count: 2
        },
        stroke: {
            curve: 'straight',
            width: [0, 0, 2, 2]
        },
        legend: {
            show: true,
            customLegendItems: ['تیم 2', 'تیم 1'],
            inverseOrder: true
        },
        title: {
            text: 'نمودار مساحت محدوده ترکیبی'
        },
        markers: {
            hover: {
                sizeOffset: 5
            }
        }
    };
    var chart = new ApexCharts(document.querySelector("#rangearea-combo"), options);
    chart.render();

})();