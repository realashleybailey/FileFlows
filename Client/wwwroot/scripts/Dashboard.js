export function InitChart(data, lblOverall, lblCurrent) {

    for (let i = 0; i < data.length; i++) {
        let worker = data[i];
        let workerWaiting = worker.status === 0;
        if (workerWaiting) {
            DestroyChart(i);
            continue;
        }
        let overall = worker.totalParts == 0 ? 100 : (worker.currentPart / worker.totalParts) * 100;
        let options = {
            chart: {
                id: 'chart-' + i,
                height: 280,
                type: "radialBar",
                foreColor: 'var(--color)',
            },
            plotOptions: {
                radialBar: {
                    hollow: {
                        margin: 5,
                        size: '48%',
                        background: 'transparent',
                    },
                    track: {
                        //show: false,
                        background: '#333',
                    },
                    startAngle: -135,
                    endAngle: 135,
                    stroke: {
                        lineCap: 'round'
                    },
                    dataLabels: {
                        total: {
                            show: true,
                            label: lblOverall,
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
            labels: [lblOverall]
        };
        if (worker.currentPartPercent > 0) {
            options.series.push(worker.currentPartPercent);
            options.labels.push(lblCurrent);
        }
        if (!worker.currentFile) {
            // currently not processing anything
            options.plotOptions.radialBar.dataLabels.total.label = '';
            options.plotOptions.radialBar.dataLabels.total.formatter = function (val) {
                return "";
            };
        }

        let updated = false;

        if (document.querySelector('.chart-' + i + ' .apexcharts-canvas')) {
            try {
                ApexCharts.exec('chart-' + i, 'updateOptions', options, false, false);
                updated = true;
            } catch (err) { }
        }

        if (updated === false) {
            new ApexCharts(document.querySelector(".chart-" + i), options).render();
        }
    }
}

export function DestroyChart(index) {
    try {
        ApexCharts.exec('chart-' + index, 'destroy');
    } catch (err) {
    }

}

export function DestroyAllCharts() {
    for (let i = 0; i < 20; i++) {
        try {
            ApexCharts.exec('chart-' + i, 'destroy');
        } catch (err) {
        }
    }
}