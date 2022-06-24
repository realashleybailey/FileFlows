export function newTreeMap(uid, args){
    if(!window.FlowCharts)
        window.FlowCharts = {};
    window.FlowCharts[uid] = new TreeMapChart(uid, args);
}

export function dispose(uid) {
    let chart = window.FlowCharts[uid];
    if(chart)
        chart.dispose();
}


export class TreeMapChart{
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
        
        if(!data?.length) {
            document.getElementById(this.uid).closest('.portlet').style.display = 'none';
            return;
        }
        this.createChart(data);
    }

    fixData(data) {
        if (!data?.length || (data[0].Name && data[0].Value) === false)
            return data;

        //statistic data, convert it
        let newData = {};
        for (let d of data) {
            if (d.Value === 'mpeg2video')
                d.Value = 'mpeg2'; // too long
            if (newData[d.Value])
                newData[d.Value] = newData[d.Value] + 1;
            else
                newData[d.Value] = 1;
        }
        data = [];
        Object.keys(newData).forEach(x => {
            data.push({x: x, y: newData[x]});
        });
        return data;
    }
    
    createChart(data)
    {        
        let options = {
            chart: {
                height: 300,
                type: 'treemap',
                background: 'transparent',
                zoom: {
                    enabled: false
                },
                toolbar: {
                    show: false
                }
            },
            colors: ['#33b2df'],
            theme: {
                mode: 'dark'
            },
            stroke:{
                colors:['#33b2df']
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