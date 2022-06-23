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
    seriesName = 'CPU Usage';
    
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
        
        let data;
        if(this.lastFetch) {
            let response = await fetch(`${this.url}?since=${this.lastFetch}`);
            data = await response.json();
        }else {
            let response = await fetch(this.url);
            data = await response.json();
        }   
        
        for(let d of data){
            if(typeof(d.x) === 'string')
                d.x = new Date(Date.parse(d.x));
        }
        
        if(this.lastFetch)
            this.data = this.data.concat(data);
        else {
            this.data = data;
            this.createTop();
        }
        this.lastFetch = this.data[this.data.length -1].x;

        let buckets = this.adjustData(this.data, 100);
        if(buckets.length !== this.data.length)
        {            
            this.buckets = buckets;
            if(this.chartBottom) {
                this.chartBottom.updateSeries([{
                    name: this.seriesName,
                    data: this.buckets
                }]);
            }else {
                this.createBottom();
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
    
    adjustData(data, desiredItems){
        let min = data[0].x;
        let max = data[data.length - 1].x;

        let timeDiff = (max - min) / 60000;
        let minutes = 0;
        if(timeDiff < 5)
            minutes = 0;
        else if(timeDiff < desiredItems)
            minutes = 1;
        else
            minutes = Math.floor(timeDiff / desiredItems);

        if(minutes === 0)
            return data;
        
        const ms = 1000 * 60 * minutes;

        // update the summary graph
        let buckets = [];
        let bucketDict = {};
        for(let d of data) {
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
        return buckets;
    }
    
    createTop(){
        let data = this.adjustData(this.data, 500);
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
                    data: data
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

    updateTopTimeout;
    
    updateTopSelection(minDate, maxDate, dontWait)
    {
        let doIt = () => {
            console.log('updateTopSelection.minDate', minDate);
            console.log('updateTopSelection.maxDate', maxDate);

            let min = minDate.getTime();
            let max = maxDate.getTime();
            let rangeData = this.data.filter(x => {
                let xTime = x.x.getTime();
                return xTime >= min && xTime <= max;
            });
            let data = this.adjustData(rangeData, 500);

            this.chartTop.updateSeries([{
                name: this.seriesName,
                data: data
            }]);
            console.log('series updated', data);
        };
        
        if(dontWait)
            doIt();
        if(this.updateTopTimeout)
            clearTimeout(this.updateTopTimeout);
        this.updateTopTimeout = setTimeout(() => doIt(), 250);
    }


    createBottom(){
        console.log('create bottom data', this.buckets);
        let d = [] ;
        let yMax = 0;

        let brushEnd = this.buckets[this.buckets.length - 1].x;
        let brushStart = new Date(brushEnd.getTime() - 5 * 60000); // -5 minutes
        if(this.buckets[0].x > brushStart)
            brushStart = this.buckets[0].x;
        for(let b of this.buckets) {
            d.push({x: b.x, y: (b.y.toFixed(1) + ' %')});
            if(b.y > yMax)
                yMax = b.y;
        }
        console.log('brush', brushStart, brushEnd);
        
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
                    },
                    xaxis: {
                        min: brushStart.getTime(),
                        max: brushEnd.getTime()
                    }
                },
                events: {
                    selection: (context, xy) => {
                        this.updateTopSelection(new Date(xy.xaxis.min), new Date(xy.xaxis.max));
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