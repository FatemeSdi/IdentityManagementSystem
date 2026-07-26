(function () {
    "use strict";

    /* basic radar chart */
    var options = {
        series: [{
            name: 'اطلاعات 1',
            data: [80, 50, 30, 40, 100, 20],
        }],
        chart: {
            height: 320,
            type: 'radar',
        },
        title: {
            text: 'نمودار راداری پایه',
            align: 'center',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
        colors: ["#589bff"],
        xaxis: {
    		categories: ['فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور']

        }
    };
    var chart = new ApexCharts(document.querySelector("#radar-basic"), options);
    chart.render();

    /* radar chart polygn fill */
    var options = {
        series: [{
            name: 'اطلاعات 1',
            data: [20, 100, 40, 30, 50, 80, 33],
        }],
        chart: {
            height: 320,
            type: 'radar',
        },
        dataLabels: {
            enabled: true
        },
        plotOptions: {
            radar: {
                size: 80,
                polygons: {
                    strokeColors: '#e9e9e9',
                }
            }
        },
        title: {
            text: 'رادار',
            align: 'center',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
        colors: ['#af6ded'],
        markers: {
            size: 4,
            colors: ['#fff'],
            strokeColor: '#af6ded',
            strokeWidth: 2,
        },
        tooltip: {
            y: {
                formatter: function (val) {
                    return val
                }
            }
        },
        xaxis: {
          categories: ['فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور', 'مهر']
        },
        yaxis: {
            tickAmount: 7,
            labels: {
                formatter: function (val, i) {
                    if (i % 2 === 0) {
                        return val
                    } else {
                        return ''
                    }
                }
            }
        }
    };
    var chart = new ApexCharts(document.querySelector("#radar-polygon"), options);
    chart.render();

})();