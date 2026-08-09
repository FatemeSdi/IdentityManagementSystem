(function () {
    "use strict";

    var options = {
        series: [{
            name: 'ساعت‌ها',
            data: [40, 35, 66, 28, 38, 55, 45],
            type: 'bar',
        },{
            name: 'آلبوم‌ها',
            data: [20, 45, 25, 60, 45, 65, 55],
            type: 'line',
        }],
        chart: {
            height: 380,
            fontFamily: 'Poppins, Arial, sans-serif',
            toolbar: {
                show: false
            }
        },
        grid: {
            show: false,
            borderColor: '#f2f6f7',
        },
        dataLabels: {
            enabled: false  
        },
        legend: {
            position: 'top',
            fontSize: '13px',
        },
        colors: ["var(--primary08)","rgba(127, 103, 257,0.8)"],
        stroke: {
            width: [1,1.8],
            curve: 'smooth',
            dashArray: [0, 4],
        },
        plotOptions: {
            bar: {
                columnWidth: "25%",
                borderRadius: 3,
				colors: {
					ranges: [{
						from: 41,
						to: 100,
						color: 'rgba(127, 103, 257, 0.8)'
					}, {
						from: 0,
						to: 40,
						color: 'var(--primary08)'
					}]
				},
            }
        },
        tooltip: {
          enabled: true,
          theme: "dark",
        },
        labels: [
			  "شنبه",
			  "یکشنبه",
			  "دوشنبه",
			  "سه شنبه",
			  "چهارشنبه",
			  "پنج شنبه",
			  "جمعه",
			],
			
    };
    var chart3 = new ApexCharts(document.querySelector("#podcast-activity"), options);
    chart3.render();

})()
