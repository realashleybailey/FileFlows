export function newPieChart(uid, args){
    if(!window.FlowCharts)
        window.FlowCharts = {};
    window.FlowCharts[uid] = new PieChartChart(uid, args);
}

export function dispose(uid) {
    let chart = window.FlowCharts[uid];
    if(chart)
        chart.dispose();
}


export class PieChartChart{
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
        data = this.fixData(data);
        this.createChart(data);
    }
    
    fixData(data) {
        if (!data?.length || (data[0].Name && data[0].Value) === false)
            return data;

        //statistic data, convert it
        let newData = {};
        for (let d of data) {
            if (newData[d.Value])
                newData[d.Value] = newData[d.Value] + 1;
            else
                newData[d.Value] = 1;
        }
        let temp = [];
        Object.keys(newData).forEach(x => {
            temp.push({
                label: x,
                value: newData[x]
            })
        });
        temp.sort((a, b) => {
            return b.value - a.value
        });

        data = {
            labels: [],
            series: []
        };
        for(let v of temp)
        {
            data.labels.push(v.label);
            data.series.push(v.value);
        }
        console.log('pie chart data', data);
        return data;
    }
    
    
    createChart(data)
    {
        if(!data?.series?.length)
            return;
        
        let options = {
            chart: {
                height: 300,
                type: 'donut',
                background: 'transparent'
            },
            theme: {
                mode: 'dark',
                monochrome: {
                    enabled: true,
                    color:'#02647e'
                }
            },
            stroke:{
                colors:['#33b2df']
            },
            colors: [
                // #33b2df , common blue
                '#33b2df',
                'rgba(51,223,85,0.65)',
                '#84004bd9',

                'var(--blue)',
                'var(--indigo)',
                'var(--cyan)',
                'var(--orange)',
                'var(--green)',
                'var(--teal)',
                'var(--teal)',
                'var(--yellow)',
                'var(--error)',
            ],
            series: data.series,
            labels: data.labels
        };
        this.chart = new ApexCharts(document.getElementById(this.uid), options);
        this.chart.render();
    }
    
    dispose() {
        this.disposed = true;      
    }
    
    
}