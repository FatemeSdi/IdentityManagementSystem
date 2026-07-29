(function () {
    "use strict";

    /* line&column chart */
    var options = {
        series: [{
            name: 'مقاله سایت',
            type: 'column',
            data: [440, 505, 414, 671, 227, 413, 201, 352, 752, 320, 257, 160]
        }, {
            name: 'فضای مجازی',
            type: 'line',
            data: [23, 42, 35, 27, 43, 22, 17, 31, 22, 22, 12, 16]
        }],
        chart: {
            height: 320,
            type: 'line',
        },
        stroke: {
            width: [0, 4]
        },
        grid: {
            borderColor: '#f2f5f7',
        },
        title: {
            text: 'منابع ترافیک',
            align: 'left',
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
        dataLabels: {
            enabled: true,
            enabledOnSeries: [1]
        },
        colors: ["#589bff", "#af6ded"],
        labels: ['01 تیر 1403', '02 تیر 1403', '03 تیر 1403', '04 تیر 1403', '05 تیر 1403', '06 تیر 1403', '07 تیر 1403', '08 تیر 1403', '09 تیر 1403', '10 تیر 1403', '11 تیر 1403', '12 تیر 1403'],
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
        yaxis: [{
            title: {
                text: 'مقالات سایت',
                style: {
                    color: "#8c9097",
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
            }
        }, {
            opposite: true,
            title: {
                text: 'فضای مجازی',
                style: {
                    color: "#8c9097",
                }
            }
        }]
    };
    var chart = new ApexCharts(document.querySelector("#mixed-linecolumn"), options);
    chart.render();

    /* multiple ys-axis chart */
    var options = {
        series: [{
            name: 'درامد',
            type: 'column',
            data: [1.4, 2, 2.5, 1.5, 2.5, 2.8, 3.8, 4.6]
        }, {
            name: 'سود',
            type: 'column',
            data: [1.1, 3, 3.1, 4, 4.1, 4.9, 6.5, 8.5]
        }, {
            name: 'هدف',
            type: 'line',
            data: [20, 29, 37, 36, 44, 45, 50, 58]
        }],
        chart: {
            height: 320,
            type: 'line',
            stacked: false
        },
        dataLabels: {
            enabled: false
        },
        stroke: {
            width: [1, 1, 4]
        },
        title: {
            text: 'تحلیل',
            align: 'left',
            offsetX: 110,
            style: {
                fontSize: '13px',
                fontWeight: 'bold',
                color: '#8c9097'
            },
        },
        grid: {
            borderColor: '#f2f5f7',
        },
        colors: ["#589bff", "#af6ded", "#fea45a"],
        xaxis: {
            categories: [1399, 1390, 1391, 1392, 1393, 1394, 1395, 1396],
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
        yaxis: [
            {
                axisTicks: {
                    show: true,
                },
                axisBorder: {
                    show: true,
                    color: '#589bff'
                },
                labels: {
                    style: {
                        colors: '#589bff',
                    }
                },
                title: {
                    text: "درامد",
                    style: {
                        color: '#589bff',
                    }
                },
                tooltip: {
                    enabled: true
                }
            },
            {
                seriesName: 'درامد',
                opposite: true,
                axisTicks: {
                    show: true,
                },
                axisBorder: {
                    show: true,
                    color: '#af6ded'
                },
                labels: {
                    style: {
                        colors: '#af6ded',
                    }
                },
                title: {
                    text: " سود ",
                    style: {
                        color: '#af6ded',
                    }
                },
            },
            {
                seriesName: 'هدف',
                opposite: true,
                axisTicks: {
                    show: true,
                },
                axisBorder: {
                    show: true,
                    color: '#fea45a'
                },
                labels: {
                    style: {
                        colors: '#fea45a',
                    },
                },
                title: {
                    text: "هدف ",
                    style: {
                        color: '#fea45a',
                    }
                }
            },
        ],
        tooltip: {
            fixed: {
                enabled: true,
                position: 'topLeft', // topRight, topLeft, bottomRight, bottomLeft
                offsetY: 30,
                offsetX: 60
            },
        },
        legend: {
            horizontalAlign: 'left',
            offsetX: 40
        }
    };
    var chart = new ApexCharts(document.querySelector("#mixed-multiple-y"), options);
    chart.render();

    /* line and area chart */
    var options = {
        series: [{
            name: 'تیم یک',
            type: 'area',
            data: [44, 55, 31, 47, 31, 43, 26, 41, 31, 47, 33]
        }, {
            name: 'تیم دو',
            type: 'line',
            data: [55, 69, 45, 61, 43, 54, 37, 52, 44, 61, 43]
        }],
        chart: {
            height: 320,
            type: 'line',
        },
        stroke: {
            curve: 'smooth'
        },
        colors: ["#589bff", "#af6ded"],
        grid: {
            borderColor: '#f2f5f7',
        },
        fill: {
            type: 'solid',
            opacity: [0.35, 1],
        },
        labels: ['تیر 01', 'تیر 02', 'تیر 03', 'تیر 04', 'تیر 05', 'تیر 06', 'تیر 07', 'تیر 08', 'تیر 09 ', 'تیر 10', 'تیر 11'],
        markers: {
            size: 0
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
        yaxis: [
            {
                title: {
                    text: ' یک',
                    style: {
                        color: "#8c9097",
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
                }
            },
            {
                opposite: true,
                title: {
                    text: ' دو',
                    style: {
                        color: "#8c9097",
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
                }
            },
        ],
        tooltip: {
            shared: true,
            intersect: false,
            y: {
                formatter: function (y) {
                    if (typeof y !== "undefined") {
                        return y.toFixed(0) + " واحد";
                    }
                    return y;
                }
            }
        }
    };
    var chart = new ApexCharts(document.querySelector("#mixed-linearea"), options);
    chart.render();

    /* line column and area chart */
    var options = {
        series: [{
            name: 'تیم یک',
            type: 'column',
            data: [23, 11, 22, 27, 13, 22, 37, 21, 44, 22, 30]
        }, {
            name: 'تیم دو',
            type: 'area',
            data: [44, 55, 41, 67, 22, 43, 21, 41, 56, 27, 43]
        }, {
            name: 'تیم سه',
            type: 'line',
            data: [30, 25, 36, 30, 45, 35, 64, 52, 59, 36, 39]
        }],
        chart: {
            height: 320,
            type: 'line',
            stacked: false,
        },
        stroke: {
            width: [0, 2, 5],
            curve: 'smooth'
        },
        plotOptions: {
            bar: {
                columnWidth: '50%'
            }
        },
        colors: ["#589bff","#af6ded","#fea45a"],
        grid: {
            borderColor: '#f2f5f7',
        },
        fill: {
            opacity: [0.85, 0.25, 1],
            gradient: {
                inverseColors: false,
                shade: 'light',
                type: "vertical",
                opacityFrom: 0.85,
                opacityTo: 0.55,
                stops: [0, 100, 100, 100]
            }
        },
        labels: ['01/01/1393', '02/01/1393', '03/01/1393', '04/01/1393', '05/01/1393', '06/01/1393', '07/01/1393',
            '08/01/1393', '09/01/1393', '10/01/1393', '11/01/1393'
        ],
        markers: {
            size: 0
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
            title: {
                text: 'واحد',
                style: {
                    color: "#8c9097",
                }
            },
            min: 0,
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
            shared: true,
            intersect: false,
            y: {
                formatter: function (y) {
                    if (typeof y !== "undefined") {
                        return y.toFixed(0) + " واحد";
                    }
                    return y;

                }
            }
        }
    };

    var chart = new ApexCharts(document.querySelector("#mixed-all"), options);
    chart.render();

})();