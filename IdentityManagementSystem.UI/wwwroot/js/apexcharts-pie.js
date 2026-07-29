"use strict";

/* basic pie chart */
var options = {
  series: [44, 55, 13, 43, 22],
  chart: {
    height: 300,
    type: "pie",
  },
  colors: ["#589bff", "#af6ded", "#fea45a", "#28d785", "#dd3a52"],
  labels: ["تیم 1", "تیم 2", "تیم 3", "تیم 3", "تیم 4"],
  legend: {
    position: "bottom",
  },
  dataLabels: {
    dropShadow: {
      enabled: false,
    },
  },
};
var chart = new ApexCharts(document.querySelector("#pie-basic"), options);
chart.render();


/* monochrome pie chart */
var options = {
  series: [25, 15, 44, 55, 41, 17],
  chart: {
    height: 308,
    type: "pie",
  },
  labels: ['فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور', 'مهر'],
  theme: {
    monochrome: {
      enabled: true,
      color: "#589bff",
    },
  },
  plotOptions: {
    pie: {
      dataLabels: {
        offset: -5,
      },
    },
  },
  title: {
    text: "نمودار دایره ای پر شده با تصویر",
    align: "center",
    style: {
      fontSize: "13px",
      fontWeight: "bold",
      color: "#8c9097",
    },
  },
  dataLabels: {
    formatter(val, opts) {
      const name = opts.w.globals.labels[opts.seriesIndex];
      return [name, val.toFixed(1) + "%"];
    },
    dropShadow: {
      enabled: false,
    },
  },
  legend: {
    show: false,
  },
};
var chart = new ApexCharts(document.querySelector("#pie-monochrome"), options);
chart.render();


/* patterned donut chart */
var options = {
  series: [44, 55, 41, 17, 15],
  chart: {
    height: 290,
    type: "donut",
    dropShadow: {
      enabled: true,
      color: "#111",
      top: -1,
      left: 3,
      blur: 3,
      opacity: 0.2,
    },
  },
  stroke: {
    width: 0,
  },
  plotOptions: {
    pie: {
      donut: {
        labels: {
          show: false,
          total: {
            showAlways: false,
            show: false,
          },
        },
      },
    },
  },
  colors: ["#589bff", "#af6ded", "#fea45a", "#28d785", "#dd3a52"],
  labels: ["کمدی", "اکشن", "علمی تخیلی", "درام", "وحشتناک"],
  dataLabels: {
    enabled: true,
    style: {
      colors: ["#111"],
    },
    background: {
      enabled: true,
      foreColor: "#fff",
      borderWidth: 0,
    },
  },
  fill: {
    type: "pattern",
    opacity: 1,
    pattern: {
      enabled: true,
      style: [
        "verticalLines",
        "squares",
        "horizontalLines",
        "circles",
        "slantedLines",
      ],
    },
  },
  states: {
    hover: {
      filter: "none",
    },
  },
  theme: {
    palette: "palette2",
  },
  title: {
    text: "ویدیو مورد علاقه",
    align: "center",
    style: {
      fontSize: "13px",
      fontWeight: "bold",
      color: "#8c9097",
    },
  },
  responsive: [
    {
      breakpoint: 480,
      options: {
        chart: {
          width: 200,
        },
        legend: {
          position: "bottom",
        },
      },
    },
  ],
};
var chart = new ApexCharts(document.querySelector("#donut-pattern"), options);
chart.render();


  function appendData() {
  var arr = chart.w.globals.series.slice()
  arr.push(Math.floor(Math.random() * (100 - 1 + 1)) + 1)
  return arr;
}

function removeData() {
  var arr = chart.w.globals.series.slice()
  arr.pop()
  return arr;
}

function randomize() {
  return chart.w.globals.series.map(function() {
      return Math.floor(Math.random() * (100 - 1 + 1)) + 1
  })
}

function reset() {
  return options.series
}

