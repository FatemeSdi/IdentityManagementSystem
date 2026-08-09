(function () {
    "use strict";

    /* basic heatmap chart */
    function generateData(count, yrange) {
        var i = 0;
        var series = [];
        while (i < count) {
            var x = 'w' + (i + 1).toString();
            var y = Math.floor(Math.random() * (yrange.max - yrange.min + 1)) + yrange.min;

            series.push({
                x: x,
                y: y
            });
            i++;
        }
        return series;
    }
    var options = {
        series: [{
            name: 'مقدار1',
            data: generateData(18, {
                min: 0,
                max: 90
            })
        },
        {
            name: 'مقدار2',
            data: generateData(18, {
                min: 0,
                max: 90
            })
        },
        {
            name: 'مقدار3',
            data: generateData(18, {
                min: 0,
                max: 90
            })
        },
        {
            name: 'مقدار4',
            data: generateData(18, {
                min: 0,
                max: 90
            })
        },
        {
            name: 'مقدار5',
            data: generateData(18, {
                min: 0,
                max: 90
            })
        },
        {
            name: 'مقدار6',
            data: generateData(18, {
                min: 0,
                max: 90
            })
        },
        {
            name: 'مقدار7',
            data: generateData(18, {
                min: 0,
                max: 90
            })
        },
        {
            name: 'مقدار8',
            data: generateData(18, {
                min: 0,
                max: 90
            })
        },
        {
            name: 'مقدار9',
            data: generateData(18, {
                min: 0,
                max: 90
            })
        }
        ],
        chart: {
            height: 350,
            type: 'heatmap',
        },
        dataLabels: {
            enabled: false
        },
        colors: ["#589bff"],
        grid: {
            borderColor: '#f2f5f7',
        },
        title: {
            text: 'هیت مپ',
            align: 'center',
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
    var chart = new ApexCharts(document.querySelector("#heatmap-basic"), options);
    chart.render();


    /* color range heatmap */
    function generateData(count, yrange) {
        var i = 0;
        var series = [];
        while (i < count) {
            var x = (i + 1).toString();
            var y = Math.floor(Math.random() * (yrange.max - yrange.min + 1)) + yrange.min;

            series.push({
                x: x,
                y: y
            });
            i++;
        }
        return series;
    }
    var options = {
        series: [{
            name: 'فروردین',
            data: generateData(20, {
                min: -30,
                max: 55
            })
        },
        {
            name: 'اردیبهشت',
            data: generateData(20, {
                min: -30,
                max: 55
            })
        },
        {
            name: 'خرداد',
            data: generateData(20, {
                min: -30,
                max: 55
            })
        },
        {
            name: 'تیر',
            data: generateData(20, {
                min: -30,
                max: 55
            })
        },
        {
            name: 'مرداد',
            data: generateData(20, {
                min: -30,
                max: 55
            })
        },
        {
            name: 'شهریور',
            data: generateData(20, {
                min: -30,
                max: 55
            })
        },
        {
            name: 'مهر',
            data: generateData(20, {
                min: -30,
                max: 55
            })
        },
        {
            name: 'ابان',
            data: generateData(20, {
                min: -30,
                max: 55
            })
        },
        {
            name: 'اذر',
            data: generateData(20, {
                min: -30,
                max: 55
            })
        }
        ],
        chart: {
            height: 350,
            type: 'heatmap',
        },
        plotOptions: {
            heatmap: {
                shadeIntensity: 0.5,
                radius: 0,
                useFillColorAsStroke: true,
                colorScale: {
                    ranges: [{
                        from: -30,
                        to: 5,
                        name: 'کم',
                        color: '#589bff'
                    },
                    {
                        from: 6,
                        to: 20,
                        name: 'متوسط',
                        color: '#af6ded'
                    },
                    {
                        from: 21,
                        to: 45,
                        name: 'زیاد',
                        color: '#fea45a'
                    },
                    {
                        from: 46,
                        to: 55,
                        name: 'خیلی زیاد',
                        color: '#28d785'
                    }
                    ]
                }
            }
        },
        dataLabels: {
            enabled: false
        },
        grid: {
            borderColor: '',
        },
        stroke: {
            width: 1
        },
        title: {
            text: 'هیت مپ',
            align: 'center',
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
    var chart = new ApexCharts(document.querySelector("#heatmap-colorrange"), options);
    chart.render();

    /* heatmap range without shades */
    var options = {
        series: [{
            name: 'مقدار1',
            data: generateData(20, {
                min: 0,
                max: 90
            })
        },
        {
            name: 'مقدار2',
            data: generateData(20, {
                min: 0,
                max: 90
            })
        },
        {
            name: 'مقدار3',
            data: generateData(20, {
                min: 0,
                max: 90
            })
        },
        {
            name: 'مقدار4',
            data: generateData(20, {
                min: 0,
                max: 90
            })
        },
        {
            name: 'مقدار5',
            data: generateData(20, {
                min: 0,
                max: 90
            })
        },
        {
            name: 'مقدار6',
            data: generateData(20, {
                min: 0,
                max: 90
            })
        },
        {
            name: 'مقدار7',
            data: generateData(20, {
                min: 0,
                max: 90
            })
        },
        {
            name: 'مقدار8',
            data: generateData(20, {
                min: 0,
                max: 90
            })
        },
        {
            name: 'مقدار8',
            data: generateData(20, {
                min: 0,
                max: 90
            })
        }
        ],
        chart: {
            height: 350,
            type: 'heatmap',
        },
        stroke: {
            width: 0
        },
        plotOptions: {
            heatmap: {
                radius: 30,
                enableShades: false,
                colorScale: {
                    ranges: [{
                        from: 0,
                        to: 50,
                        color: '#589bff'
                    },
                    {
                        from: 51,
                        to: 100,
                        color: '#af6ded'
                    },
                    ],
                },

            }
        },
        grid: {
            borderColor: '#f2f5f7',
        },
        dataLabels: {
            enabled: true,
            style: {
                colors: ['#fff']
            }
        },
        xaxis: {
            type: 'category',
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
        title: {
            text: 'گرد شده',
            align: 'center',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
    };
    var chart = new ApexCharts(document.querySelector("#heatmap-range"), options);
    chart.render();

})();