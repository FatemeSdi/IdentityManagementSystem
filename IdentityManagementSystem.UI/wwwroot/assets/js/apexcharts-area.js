(function () {
    "use strict";

    /* basic area chart */
    var options = {
        series: [{
            name: "سهام ABC",
            data: [10,20,10,40,100,60,70,80,90,100,50,90]
        }],
        chart: {
            type: 'area',
            height: 320,
            zoom: {
                enabled: false
            }
        },
        dataLabels: {
            enabled: false
        },
        stroke: {
            curve: 'straight',
        },
        subtitle: {
            text: 'حرکات قیمت',
            align: 'left'
        },
        grid: {
            borderColor: '#f2f5f7',
        },
		labels: ['فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور', 'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'],
        title: {
            text: 'تحلیل سهام',
            align: 'left',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
        colors: ['#589bff'],
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
            opposite: true,
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
        legend: {
            horizontalAlign: 'left'
        }
    };
    var chart = new ApexCharts(document.querySelector("#area-basic"), options);
    chart.render();

    /* spline area chart */
    var options = {
        series: [{
            name: 'تست 1',
            data: [31, 40, 28, 51, 42, 109, 100]
        }, {
            name: 'تست 2',
            data: [11, 32, 45, 32, 34, 52, 41]
        }],
        chart: {
            height: 320,
            type: 'area'
        },
        colors: ["#589bff", "#af6ded"],
        dataLabels: {
            enabled: false
        },
        stroke: {
            curve: 'smooth'
        },
        grid: {
            borderColor: '#f2f5f7',
        },
        xaxis: {
            type: 'datetime',
            categories: ["1403-09-19T00:00:00.000Z", "1403-09-19T01:30:00.000Z", "1403-09-19T02:30:00.000Z", "1403-09-19T03:30:00.000Z", "1403-09-19T04:30:00.000Z", "1403-09-19T05:30:00.000Z", "1403-09-19T06:30:00.000Z"],
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
                    cssClass: 'apexcharts-xaxis-label',
                },
            }
        },
        tooltip: {
            x: {
                format: 'dd/MM/yy HH:mm'
            },
        },
    };
    var chart = new ApexCharts(document.querySelector("#area-spline"), options);
    chart.render();

    /* negative values chart */
    var options = {
        series: [{
            name: 'شمال',
            data: [{
                x: 1996,
                y: 322
            },
            {
                x: 1997,
                y: 324
            },
            {
                x: 1998,
                y: 329
            },
            {
                x: 1999,
                y: 342
            },
            {
                x: 2000,
                y: 348
            },
            {
                x: 2001,
                y: 334
            },
            {
                x: 2002,
                y: 325
            },
            {
                x: 2003,
                y: 316
            },
            {
                x: 2004,
                y: 318
            },
            {
                x: 2005,
                y: 330
            },
            {
                x: 2006,
                y: 355
            },
            {
                x: 2007,
                y: 366
            },
            {
                x: 2008,
                y: 337
            },
            {
                x: 2009,
                y: 352
            },
            {
                x: 2010,
                y: 377
            },
            {
                x: 2011,
                y: 383
            },
            {
                x: 2012,
                y: 344
            },
            {
                x: 2013,
                y: 366
            },
            {
                x: 2014,
                y: 389
            },
            {
                x: 2015,
                y: 334
            }
            ]
        }, {
            name: 'جنوب',
            data: [
                {
                    x: 1996,
                    y: 162
                },
                {
                    x: 1997,
                    y: 90
                },
                {
                    x: 1998,
                    y: 50
                },
                {
                    x: 1999,
                    y: 77
                },
                {
                    x: 2000,
                    y: 35
                },
                {
                    x: 2001,
                    y: -45
                },
                {
                    x: 2002,
                    y: -88
                },
                {
                    x: 2003,
                    y: -120
                },
                {
                    x: 2004,
                    y: -156
                },
                {
                    x: 2005,
                    y: -123
                },
                {
                    x: 2006,
                    y: -88
                },
                {
                    x: 2007,
                    y: -66
                },
                {
                    x: 2008,
                    y: -45
                },
                {
                    x: 2009,
                    y: -29
                },
                {
                    x: 2010,
                    y: -45
                },
                {
                    x: 2011,
                    y: -88
                },
                {
                    x: 2012,
                    y: -132
                },
                {
                    x: 2013,
                    y: -146
                },
                {
                    x: 2014,
                    y: -169
                },
                {
                    x: 2015,
                    y: -184
                }
            ]
        }],
        chart: {
            type: 'area',
            height: 323
        },
        dataLabels: {
            enabled: false
        },
        stroke: {
            curve: 'straight'
        },

        title: {
            text: 'منطقه',
            align: 'left',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            }
        },
        xaxis: {
            type: 'datetime',
            axisBorder: {
                show: false
            },
            axisTicks: {
                show: false
            },
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
            tickAmount: 4,
            floating: false,

            labels: {
                style: {
                    colors: '#8c9097',
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-yaxis-label',
                },
                offsetY: -7,
                offsetX: 0,
            },
            axisBorder: {
                show: false,
            },
            axisTicks: {
                show: false
            }
        },
        colors: ["#589bff", "#af6ded"],
        fill: {
            opacity: 0.5
        },
        tooltip: {
            x: {
                format: "yyyy",
            },
            fixed: {
                enabled: false,
                position: 'topRight'
            }
        },
        grid: {
            borderColor: '#f2f5f7',
            yaxis: {
                lines: {
                    offsetX: -30
                }
            },
            padding: {
                left: 20
            }
        }
    };

    var chart = new ApexCharts(document.querySelector("#area-negative"), options);
    chart.render();



    /* stacked area chart */
    var generateDayWiseTimeSeries = function (baseval, count, yrange) {
        var i = 0;
        var series = [];
        while (i < count) {
            var x = baseval;
            var y = Math.floor(Math.random() * (yrange.max - yrange.min + 1)) + yrange.min;
            series.push([x, y]);
            baseval += 86400000;
            i++;
        }
        return series;
    }
    


    var resetCssClasses = function (activeEl) {
        var els = document.querySelectorAll('button')
        Array.prototype.forEach.call(els, function (el) {
            el.classList.remove('active')
        })
        activeEl.target.classList.add('active')
    }
  
   

})();