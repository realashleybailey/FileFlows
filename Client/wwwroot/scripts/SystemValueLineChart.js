export function newSystemValueLineChart(uid, args){
    if(!window.FlowCharts)
        window.FlowCharts = {};
    window.FlowCharts[uid] = new SystemValueLineChart(uid, args);
}

export function updateData(uid, data){
    let chart = window.FlowCharts[uid];
    console.log('new data', data);
    if(!chart){
        console.log('chart not found!');
        return;
    }
    chart.updateData(data);
}

export function dispose(uid) {
    let chart = window.FlowCharts[uid];
    if(chart)
        chart.dispose();
}


export class SystemValueLineChart{
    uid;
    bottomUid;
    topUid;
    chartBottom;
    sizeData;
    data;
    buckets;
    url;
    lastFetch;
    timer;
    disposed;
    
    constructor(uid, args) {
        console.log('uid', uid);
        this.uid = uid;
        
        this.bottomUid = uid + '-bottom';
        this.topUid = uid + '-top';
        this.sizeData = !!args?.sizeData;
        this.url = args.url;
        
        this.getData();
    }
    
    async getData() {
        if(this.disposed)
            return; 
        
        if(this.lastFetch) {
            let response = await fetch(`${this.url}?since=${this.lastFetch}`);
            let data = await response.json();
            this.data = this.data.concat(data);
        }else {
            let response = await fetch(this.url);
            this.data = await response.json();
            this.createTop();
        }        
        this.lastFetch = this.data[this.data.length -1].x;

        let min = Date.parse(this.data[0].x);
        let max = Date.parse(this.data[this.data.length - 1].x);
        let timeDiff = (max - min) / 60000;
        let minutes = 0;
        if(timeDiff < 5)
            minutes = 0;
        else if(timeDiff < 100)
            minutes = 1;
        else 
            minutes = Math.floor(timeDiff / 100);
                
        if(minutes > 0)
        {            
            const ms = 1000 * 60 * minutes;
            
            // update the summary graph
            let buckets = [];
            let bucketDict = {};
            for(let d of this.data) {
                let dt = new Date(Date.parse(d.x));
                let thirtyMins = new Date(Math.floor(dt.getTime() / ms) * ms);
                if(bucketDict[thirtyMins] == null) {
                    bucketDict[thirtyMins] = {x: thirtyMins, y: d.y, t: d.y, c: 1};
                    buckets.push(bucketDict[thirtyMins]);
                }
                else {
                    let b = bucketDict[thirtyMins];
                    b.t += d.y;
                    ++b.c; 
                    b.y = b.t / b.c;
                }
            }            
            if(!this.buckets){
                this.buckets = buckets;
                this.createBottom();
            }else{
                this.buckets = buckets;                
            }
        }
        if(this.timer)
            clearTimeout(this.timer);
        if(!this.disposed)
            this.timer = setTimeout(() => this.getData(), 5000);
    }
    
    updateData(data){
        let animate = false;
        let actualData = [];
        for(let d of data){
            if(typeof(d.time) === 'string')
                d.time = new Date(Date.parse(d.time))
            actualData.push({ x: d.time, y: d.value});
        }
        console.log('updating chart data', actualData);
        // this.chartTop.updateSeries([{
        //     data: actualData
        // }], animate);
        this.chartTop.appendData([{
            data: actualData
        }]);
    }
    
    createTop(){
        var options = {
            chart: {
                id: this.topUid,
                height: 100,
                type: "area",
                background: 'transparent',
                toolbar: {
                    autoSelected: 'pan',
                    show:false    
                },
                sparkline: {
                    enabled: true
                }
            },
            theme: {
                mode: 'dark',
                palette: 'palette3'
            },
            dataLabels: {
                enabled: false
            },
            series: [
                {
                    name: "CPU Usage",
                    data: this.data
                }
            ],
            grid: {
              padding: {
                  top: 0,
                  right:0,
                  bottom: 0,
                  left:0,
              },
              show:false
            },
            stroke: {
                curve: 'smooth',
                width: 1
            },
            fill: {
                type: "gradient",
                gradient: {
                    OpacityFrom: 0.55,
                    opacityTo: 0
                }
            },
            xaxis: {
                type:'datetime',
                axisTicks : {
                    show: false
                },
                labels : {
                    show: false
                }
            },
            yaxis: {
                show: false,
            },
            markers: {
                colors: ["#00BAEC"],
                strokeColors: "#00BAEC",
                strokeWidth: 3  
            },
            tooltip: {
                x: {
                    format: 'h:mm:ss ttt, d MMM yyyy', 
                    show:false                    
                },
                y: {
                    formatter: this.sizeData ?
                        (value, opts) => {
                            if (value === undefined) {
                                return '';
                            }
                            let sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
                            let order = 0;
                            while (value >= 1000 && order < sizes.length - 1) {
                                order++;
                                value = value / 1000;
                            }
                            return value.toFixed(2) + ' ' + sizes[order];
                        }
                        :
                        (value, opts) => {
                            if (value === undefined) {
                                return '';
                            }
                            return value.toFixed(1) + ' %';
                        }
                }
            }
        };

        this.chartTop = new ApexCharts(document.getElementById(this.topUid), options);
        this.chartTop.render();
    }


    createBottom(){
        console.log('create bottom data', this.buckets);
        let d = [] ;
        let yMax = 0;
        for(let b of this.buckets) {
            d.push({x: b.x, y: (b.y.toFixed(1) + ' %')});
            if(b.y > yMax)
                yMax = b.y;
        }
        var options = {
            chart: {
                height: 30,
                id: this.bottomUid,
                type: 'bar',
                background: 'transparent',
                toolbar: {
                    show:false
                },
                sparkline: {
                    enabled: true
                },
                animations: {
                    enabled: false
                },
                brush: {
                    target: this.topUid,
                    enabled: true
                },
                selection: {
                    enabled: true,
                    fill: {
                        color: "#fff",
                        opacity: 0.4
                    }
                }
            },
            markers: {
                size: 0
            },
            dataLabels: {
              enabled: false  
            },
            theme: {
                mode: 'dark',
                palette: 'palette3'
            },
            grid: {
                padding: {
                    top: 0,
                    right:0,
                    bottom: 0,
                    left:0,
                },
                show:false
            },
            series: [
                {
                    name: 'CPU Usage',
                    data: d
                }
            ],
            colors: [
              'var(--accent)'  
            ],
            stroke: {
                width:2
            },
            xaxis: {
                type:'datetime',
                axisTicks : {
                    show: false
                },
                labels : {
                    show: false
                }
            },
            yaxis: {
                min:0,
                max: yMax,
                show: false
            }
        };

        this.chartBottom = new ApexCharts(document.getElementById(this.bottomUid), options);
        this.chartBottom.render();
    }
    
    dispose() {
        this.disposed = true;      
        console.log('disposed!!!');
    }
    
    
}