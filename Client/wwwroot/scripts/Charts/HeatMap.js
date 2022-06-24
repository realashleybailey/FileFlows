export function newHeatMap(uid, args){
    if(!window.FlowCharts)
        window.FlowCharts = {};
    window.FlowCharts[uid] = new HeatMapChart(uid, args);
}

export function dispose(uid) {
    let chart = window.FlowCharts[uid];
    if(chart)
        chart.dispose();
}


export class HeatMapChart{
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
        if(!data?.length) {
            document.getElementById(this.uid).closest('.portlet').style.display = 'none';
            return;
        }
        this.createChart(data);
    }
    
    createChart(data){
        let options = {
            series: data,
            chart: {
                type: 'heatmap',
                background: 'transparent',
                zoom: {
                    enabled: false
                },
                toolbar: {
                    show: false
                }
            },
            theme: {
                mode: 'dark',
                palette: 'palette6'
            },
            grid: {
                borderColor: '#90A4AE33'
            },
            dataLabels: {
                enabled: false
            },
            colors: ["#ff0090"],
            plotOptions: {
                heatmap: {
                    shadeIntensity: 0.7,
                    radius: 0,
                    useFillColorAsStroke: true
                }
            },
        };
        this.chart = new ApexCharts(document.getElementById(this.uid), options);
        this.chart.render();
    }
    
    dispose() {
        this.disposed = true;      
    }
    
    
}