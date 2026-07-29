(function () {
    "use strict";

    /* basic line chart */
    var options = {
        series: [{
            name: "کامپیوتر",
            data: [10, 41, 35, 51, 49, 62, 69, 91, 148]
        }],
        chart: {
            height: 320,
            type: 'line',
            zoom: {
                enabled: false
            }
        },
        colors: ['#589bff'],
        dataLabels: {
            enabled: false
        },
        stroke: {
            curve: 'straight',
            width: 3,
        },
        grid: {
            borderColor: '#f2f5f7',
        },
        title: {
            text: 'روندهای محصول بر اساس ماه',
            align: 'left',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
        xaxis: {
    		categories: ['فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور', 'مهر', 'آبان', 'آذر'],
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
    var chart = new ApexCharts(document.querySelector("#line-chart"), options);
    chart.render();

    /* line with data labels */
    var options = {
        series: [
            {
                name: "بالا - 1400",
                data: [28, 29, 33, 36, 32, 32, 33]
            },
            {
                name: "کم - 1390",
                data: [12, 11, 14, 18, 17, 13, 13]
            }
        ],
        chart: {
            height: 320,
            type: 'line',
            dropShadow: {
                enabled: true,
                color: '#000',
                top: 18,
                left: 7,
                blur: 10,
                opacity: 0.2
            },
            toolbar: {
                show: false
            }
        },
        colors: ['#589bff', '#af6ded'],
        dataLabels: {
            enabled: true,
        },
        stroke: {
            curve: 'smooth'
        },
        title: {
            text: 'میانگین دما',
            align: 'left',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
        grid: {
            borderColor: '#f2f5f7',
        },
        markers: {
            size: 1
        },
        xaxis: {
    		categories: ['فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور', 'مهر'],
            title: {
                text: 'ماه',
                fontSize: '13px',
                fontWeight: 'bold',
                style: {
                    color: "#8c9097"
                }
            },
            labels: {
                show: true,
                style: {
                    colors: "#8c9097",
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-xaxis-label',
                },
            },
        },
        yaxis: {
            title: {
                text: 'دما',
                fontSize: '13px',
                fontWeight: 'bold',
                style: {
                    color: "#8c9097"
                }
            },
            labels: {
                show: true,
                style: {
                    colors: "#8c9097",
                    fontSize: '11px',
                    fontWeight: 600,
                    cssClass: 'apexcharts-yaxis-label',
                },
            },
            min: 5,
            max: 40
        },
        legend: {
            position: 'top',
            horizontalAlign: 'right',
            offsetX: -10
        }
    };
    var chart = new ApexCharts(document.querySelector("#line-chart-datalabels"), options);
    chart.render();

    

    /* stepline chart */
    var options = {
        series: [{
            name: 'تست',
            data: [34, 44, 54, 21, 12, 43, 33, 23, 66, 66, 58]
        }],
        chart: {
            type: 'line',
            height: 345
        },
        stroke: {
            curve: 'stepline',
        },
        grid: {
            borderColor: '#f2f5f7',
        },
        dataLabels: {
            enabled: false
        },
        colors: ["#589bff"],
        title: {
            text: 'نمودار مرحله ای',
            align: 'left'
        },
        markers: {
            hover: {
                sizeOffset: 4
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
    var chart2 = new ApexCharts(document.querySelector("#stepline-chart"), options);
    chart2.render();

   
    /* missing/null values chart */
    var options = {
        series: [{
            name: 'احسان',
            data: [5, 5, 10, 8, 7, 5, 4, null, null, null, 10, 10, 7, 8, 6, 9]
        }, {
            name: 'محسن',
            data: [10, 15, null, 12, null, 10, 12, 15, null, null, 12, null, 14, null, null, null]
        }, {
            name: 'علی',
            data: [null, null, null, null, 3, 4, 1, 3, 4, 6, 7, 9, 5, null, null, null]
        }],
        chart: {
            height: 320,
            type: 'line',
            zoom: {
                enabled: false
            },
            animations: {
                enabled: false
            }
        },
        grid: {
            borderColor: '#f2f5f7',
        },
        stroke: {
            width: [3, 3, 2],
            curve: 'straight'
        },
        colors: ["#589bff", "#af6ded", "#fea45a"],
        labels: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
        title: {
            text: 'اطلاعات نامشخص',
            align: 'left',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
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
    var chart = new ApexCharts(document.querySelector("#null-chart"), options);
    chart.render();

    /* syncing charts */
    // Replace Math.random() with a pseudo-random number generator to get reproducible results in e2e tests
    // Based on https://gist.github.com/blixt/f17b47c62508be59987b
    Apex = {
        chart: {
            height: 160,
        },
        dataLabels: {
            enabled: false
        },
        stroke: {
            curve: 'straight'
        },
        toolbar: {
            tools: {
                selection: false
            }
        },
        markers: {
            size: 6,
            hover: {
                size: 10
            }
        },
        tooltip: {
            followCursor: false,
            theme: 'dark',
            x: {
                show: false
            },
            marker: {
                show: false
            },
            y: {
                title: {
                    formatter: function () {
                        return ''
                    }
                }
            }
        },
        grid: {
            clipMarkers: false
        },
        yaxis: {
            tickAmount: 2
        },
        xaxis: {
            type: 'datetime'
        },
    }
    function generateDayWiseTimeSeries(baseval, count, yrange) {
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
    
  
    /* dashed chart */
    var options = {
        series: [{
            name: "مدت زمان جلسه",
            data: [45, 52, 38, 24, 33, 26, 21, 20, 6, 8, 15, 10]
        },
        {
            name: "بازدید صفحه",
            data: [35, 41, 62, 42, 13, 18, 29, 37, 36, 51, 32, 35]
        },
        {
            name: 'کل بازدیدکنندگان',
            data: [87, 57, 74, 99, 75, 38, 62, 47, 82, 56, 45, 47]
        }
        ],
        chart: {
            height: 320,
            type: 'line',
            zoom: {
                enabled: false
            },
        },
        dataLabels: {
            enabled: false
        },
        stroke: {
            width: [3, 4, 3],
            curve: 'straight',
            dashArray: [0, 8, 5]
        },
        colors: ["#589bff", "#af6ded", "#fea45a"],
        title: {
            text: 'آمار صفحه',
            align: 'left',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
        legend: {
            tooltipHoverFormatter: function (val, opts) {
                return val + ' - ' + opts.w.globals.series[opts.seriesIndex][opts.dataPointIndex] + ''
            }
        },
        markers: {
            size: 0,
            hover: {
                sizeOffset: 6
            }
        },
        xaxis: {
            categories: ['01 تیر', '02 تیر', '03 تیر', '04 تیر', '05 تیر', '06 تیر', '07 تیر', '08 تیر', '09 تیر',
                '10 تیر', '11 تیر', '12 تیر'
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
                    cssClass: 'apexcharts-xaxis-label',
                },
            }
        },
        tooltip: {
            y: [
                {
                    title: {
                        formatter: function (val) {
                            return val ;
                        }
                    }
                },
                {
                    title: {
                        formatter: function (val) {
                            return val ;
                        }
                    }
                },
                {
                    title: {
                        formatter: function (val) {
                            return val;
                        }
                    }
                }
            ]
        },
        grid: {
            borderColor: '#f1f1f1',
        }
    };
    var chart = new ApexCharts(document.querySelector("#dashed-chart"), options);
    chart.render();

    /* real time chart */
    var lastDate = 0;
    var data = []
    var TICKINTERVAL = 86400000
    let XAXISRANGE = 777600000
    function getDayWiseTimeSeries(baseval, count, yrange) {
        var i = 0;
        while (i < count) {
            var x = baseval;
            var y = Math.floor(Math.random() * (yrange.max - yrange.min + 1)) + yrange.min;
            data.push({
                x, y
            });
            lastDate = baseval
            baseval += TICKINTERVAL;
            i++;
        }
    }
    getDayWiseTimeSeries(new Date('11 Feb 2017 GMT').getTime(), 10, {
        min: 10,
        max: 90
    })
    function getNewSeries(baseval, yrange) {
        var newDate = baseval + TICKINTERVAL;
        lastDate = newDate
        for (var i = 0; i < data.length - 10; i++) {
            // IMPORTANT
            // we reset the x and y of the data which is out of drawing area
            // to prevent memory leaks
            data[i].x = newDate - XAXISRANGE - TICKINTERVAL
            data[i].y = 0
        }
        data.push({
            x: newDate,
            y: Math.floor(Math.random() * (yrange.max - yrange.min + 1)) + yrange.min
        })
    }
    function resetData() {
        // Alternatively, you can also reset the data at certain intervals to prevent creating a huge series
        data = data.slice(data.length - 10, data.length);
    }
    

    /* brush chart */
    function generateDayWiseTimeSeries(baseval, count, yrange) {
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
  
  

})();