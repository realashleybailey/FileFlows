export function newBoxPlot(uid, args){
    if(!window.FlowCharts)
        window.FlowCharts = {};
    window.FlowCharts[uid] = new BoxPlotChart(uid, args);
}

export function dispose(uid) {
    let chart = window.FlowCharts[uid];
    if(chart)
        chart.dispose();
}


export class BoxPlotChart{
    uid;
    data;
    url;
    timer;
    disposed;
    seriesName;
    chart;
        
    constructor(uid, args) {
        this.uid = uid;
        
        this.url = args.url;
        this.seriesName = args.title;
        
        this.getData();
    }
    
    async getData() {
        if(this.disposed)
            return; 
        
        let response = await fetch(this.url);
        let data = await response.json();
        this.createChart(data);
    }
    
    createChart(data){
        let options = {
            chart: {
                height: 300,
                type: 'boxPlot',
                background: 'transparent',
                zoom: {
                    enabled: false
                },
                toolbar: {
                    show: false
                }
            },
            plotOptions: {
                boxPlot: {
                    colors: {
                        upper: '#ff0090',
                        lower: '#84004bd9'
                    }
                }
            },
            theme: {
                mode: 'dark'
            },stroke: {
                colors: ['#ffffff']
            },
            grid: {
                borderColor: '#90A4AE33'
            },
            series: [{
                data:data
            }]
        };
        this.chart = new ApexCharts(document.getElementById(this.uid), options);
        this.chart.render();
    }
    
    dispose() {
        this.disposed = true;      
    }
    
    
}