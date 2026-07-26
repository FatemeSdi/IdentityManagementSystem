(function () {
    "use strict";

    /* basic treemap chart */
    var options = {
        series: [
            {
                data: [
                    {
                        x: 'تهران',
                        y: 218
                    },
                    {
                        x: 'تهران',
                        y: 149
                    },
                    {
                        x: 'تهران',
                        y: 184
                    },
                    {
                        x: 'تهران',
                        y: 55
                    },
                    {
                        x: 'تهران',
                        y: 84
                    },
                    {
                        x: 'تهران',
                        y: 31
                    },
                    {
                        x: 'تهران',
                        y: 70
                    },
                    {
                        x: 'تهران',
                        y: 30
                    },
                    {
                        x: 'تهران',
                        y: 44
                    },
                    {
                        x: 'تهران',
                        y: 68
                    },
                    {
                        x: 'تهران',
                        y: 28
                    },
                    {
                        x: 'Indore',
                        y: 19
                    },
                    {
                        x: 'تهران',
                        y: 29
                    }
                ]
            }
        ],
        legend: {
            show: false
        },
        chart: {
            height: 350,
            type: 'treemap'
        },
        colors: ["#589bff"],
        title: {
            text: 'نمودار اصلی نقشه درختی',
            align: 'center',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
    };
    var chart = new ApexCharts(document.querySelector("#treemap-basic"), options);
    chart.render();

    /* multi dimensional treemap chart */
    var options = {
        series: [
            {
                name: 'Desktops',
                data: [
                    {
                        x: 'تهران',
                        y: 10
                    },
                    {
                        x: 'تهران',
                        y: 60
                    },
                    {
                        x: 'تهران',
                        y: 41
                    }
                ]
            },
            {
                name: 'Mobile',
                data: [
                    {
                        x: 'تهران',
                        y: 10
                    },
                    {
                        x: 'تهران',
                        y: 20
                    },
                    {
                        x: 'تهران',
                        y: 51
                    },
                    {
                        x: 'تهران',
                        y: 30
                    },
                    {
                        x: 'تهران',
                        y: 20
                    },
                    {
                        x: 'تهران',
                        y: 30
                    }
                ]
            }
        ],
        colors: ["#589bff", "#af6ded"],
        legend: {
            show: false
        },
        chart: {
            height: 350,
            type: 'treemap'
        },
        title: {
            text: 'نمودار درختی چند بعدی',
            align: 'center',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
    };
    var chart = new ApexCharts(document.querySelector("#treemap-multi"), options);
    chart.render();

    /* distributed treemap chart */
    var options = {
        series: [
            {
                data: [
                    {
                        x: 'تهران',
                        y: 218
                    },
                    {
                        x: 'تهران',
                        y: 149
                    },
                    {
                        x: 'تهران',
                        y: 184
                    },
                    {
                        x: 'تهران',
                        y: 55
                    },
                    {
                        x: 'تهران',
                        y: 84
                    },
                    {
                        x: 'تهران',
                        y: 31
                    },
                    {
                        x: 'تهران',
                        y: 70
                    },
                    {
                        x: 'تهران',
                        y: 30
                    },
                    {
                        x: 'تهران',
                        y: 44
                    },
                    {
                        x: 'تهران',
                        y: 68
                    },
                    {
                        x: 'تهران',
                        y: 28
                    },
                    {
                        x: 'تهران',
                        y: 19
                    },
                    {
                        x: 'تهران',
                        y: 29
                    }
                ]
            }
        ],
        legend: {
            show: false
        },
        chart: {
            height: 350,
            type: 'treemap'
        },
        title: {
            text: 'نمودار درختی توزیع شده',
            align: 'center',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
        colors: [
            '#589bff',
            '#fe7c58',
            '#fea45a',
            '#a66a5e',
            '#a65e9a',
            '#3a7adb',
            '#dd3a52',
            '#28d785',
            '#007c7c',
            '#2dce89',
            '#EF6537',
            '#8c9097'
        ],
        plotOptions: {
            treemap: {
                distributed: true,
                enableShades: false
            }
        }
    };
    var chart = new ApexCharts(document.querySelector("#treemap-distributed"), options);
    chart.render();

    /* treemap chart with color ranges */
    var options = {
        series: [
            {
                data: [
                    {
                        x: 'تهران',
                        y: 1.2
                    },
                    {
                        x: 'تهران',
                        y: 0.4
                    },
                    {
                        x: 'تهران',
                        y: -1.4
                    },
                    {
                        x: 'تهران',
                        y: 2.7
                    },
                    {
                        x: 'تهران',
                        y: -0.3
                    },
                    {
                        x: 'تهران',
                        y: 5.1
                    },
                    {
                        x: 'تهران',
                        y: -2.3
                    },
                    {
                        x: 'JNJ',
                        y: 2.1
                    },
                    {
                        x: 'تهران',
                        y: 0.3
                    },
                    {
                        x: 'TRV',
                        y: 0.12
                    },
                    {
                        x: 'تهران',
                        y: -2.31
                    },
                    {
                        x: 'تهران',
                        y: 3.98
                    },
                    {
                        x: 'تهران',
                        y: 1.67
                    }
                ]
            }
        ],
        legend: {
            show: false
        },
        chart: {
            height: 350,
            type: 'treemap'
        },
        title: {
            text: 'نقشه درختی با مقیاس رنگ',
            align: 'center',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
        dataLabels: {
            enabled: true,
            style: {
                fontSize: '12px',
            },
            formatter: function (text, op) {
                return [text, op.value]
            },
            offsetY: -4
        },
        plotOptions: {
            treemap: {
                enableShades: true,
                shadeIntensity: 0.5,
                reverseNegativeShade: true,
                colorScale: {
                    ranges: [
                        {
                            from: -6,
                            to: 0,
                            color: '#589bff'
                        },
                        {
                            from: 0.001,
                            to: 6,
                            color: '#af6ded'
                        }
                    ]
                }
            }
        }
    };
    var chart = new ApexCharts(document.querySelector("#treemap-colorranges"), options);
    chart.render();

})();