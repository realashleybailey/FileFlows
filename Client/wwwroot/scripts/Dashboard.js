export function InitChart(data) {
    console.log('Dashboard_InitChart!!!!', data);

    for (let i = 0; i < data.length; i++) {
        var worker = data[i];
        var overall = worker.totalParts == 0 ? 100 : (worker.currentPart / worker.totalParts) * 100;
        let options = {
            chart: {
                id: 'chart-' + i,
                height: 280,
                type: "radialBar",
            },
            plotOptions: {
                radialBar: {
                    dataLabels: {
                        total: {
                            show: true,
                            label: 'Overall',
                            formatter: function (val) {
                                return +(parseFloat(overall).toFixed(2)) + ' %';
                            }
                        },
                        value: {
                            formatter: function (val) {
                                return +(parseFloat(val).toFixed(2)) + ' %';
                            }
                        }

                    }
                }
            },
            series: [overall],
            labels: ['Overall']
        };
        if (worker.currentPartPercent > 0) {
            options.series.push(worker.currentPartPercent);
            options.labels.push('Current');
        }
        if (!worker.currentFile) {
            // currently not processing anything
            options.plotOptions.radialBar.dataLabels.total.label = 'None';
            options.plotOptions.radialBar.dataLabels.total.formatter = function (val) {
                return "";
            };
        }

        try {
            ApexCharts.exec('chart-' + i, 'updateOptions', options, false, false);
        } catch (err) {
            new ApexCharts(document.querySelector(".chart-" + i), options).render();
        }
    }
}