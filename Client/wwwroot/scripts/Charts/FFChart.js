export function initDashboard(portlets){
    console.log('init dashboard!', portlets);
    if(!portlets)
        return;
    
    let dashboard = document.querySelector('.dashboard.grid-stack');
    dashboard.textContent = '';
    
    for(let p of portlets)
    {
        let div = document.createElement("div");
        div.setAttribute('id', p.uid);
        div.className = 'grid-stack-item portlet';
        div.setAttribute('gs-w', p.width);
        div.setAttribute('gs-h', p.height);
        div.setAttribute('gs-x', p.x);
        div.setAttribute('gs-y', p.y);
        
        let title = document.createElement('div');
        div.appendChild(title);
        title.className = 'title draghandle';
        let icon = document.createElement('i');
        title.appendChild(icon);
        icon.className = p.icon;
        let spanTitle = document.createElement('span');
        title.appendChild(spanTitle);
        spanTitle.innerText = p.name;
        
        let content = document.createElement('div');
        content.className = 'content';
        div.appendChild(content);
        if(p.type === 105){
            let top = document.createElement('div');
            top.setAttribute('id', p.uid + '-top');
            content.appendChild(top);

            let bottom = document.createElement('div');
            bottom.setAttribute('id', p.uid + '-bottom');
            content.appendChild(bottom);
        }
        else
        {
            let chart = document.createElement('div');
            chart.setAttribute('id', p.uid + '-chart');
            content.appendChild(chart);            
        }
        dashboard.appendChild(div);
        newChart(p.type, p.uid, { url: p.url, flags: p.flags});
    }
    intDashboardActual('default');
}

function intDashboardActual(uid) {
    let dashboardData = localStorage.getItem('dashboard-' + uid);

    if(dashboardData)
    {
        try {
            dashboardData = JSON.parse(dashboardData);
            for (let item of dashboardData) {
                let ele = document.getElementById(item.id);
                if (!ele) {
                    console.log('element not found', item, item.id);
                    continue;
                }
                ele.setAttribute('gs-x', item.x);
                ele.setAttribute('gs-y', item.y);
                ele.setAttribute('gs-w', item.w);
                ele.setAttribute('gs-h', item.h);
            }
        }catch(err){
            // can throw if the saved data is corrupt, silent fail to defaults
        }
    }

    var grid = GridStack.init({
        cellHeight:170,
        handle: '.draghandle'
    });

    let saveGrid = () => {
        let data = [];
        for(let ele of document.querySelectorAll('.grid-stack-item')){
            let id = ele.id;
            let x = parseInt(ele.getAttribute('gs-x'), 10);
            let y = parseInt(ele.getAttribute('gs-y'), 10);
            let w = parseInt(ele.getAttribute('gs-w'), 10);
            let h = parseInt(ele.getAttribute('gs-h'), 10);
            data.push({
                id:id, x: x, y:y, w:w, h:h
            });
        }
        localStorage.setItem('dashboard-' + uid, JSON.stringify(data));
    }

    grid.on('resizestop', (e, el) => {
        window.dashboardElementResized.args = e;
        el.dispatchEvent(window.dashboardElementResized);
        saveGrid();
    });
}

export function newChart(type, uid, args){
    if(!window.FlowCharts)
        window.FlowCharts = {};
    args.type = type;
    if(type == 'BoxPlot' || type === 101)
        window.FlowCharts[uid] = new BoxPlotChart(uid, args);
    else if(type == 'HeatMap' || type === 102)
        window.FlowCharts[uid] = new HeatMapChart(uid, args);
    else if(type == 'PieChart' || type === 103)
        window.FlowCharts[uid] = new PieChartChart(uid, args);
    else if(type == 'TreeMap' || type === 104)
        window.FlowCharts[uid] = new TreeMapChart(uid, args);
    else if(type == 'TimeSeries' || type === 105)
        window.FlowCharts[uid] = new TimeSeriesChart(uid, args);
    else 
        console.log('unknown type: ' + type);
    
}

export function dispose(uid) {
    let chart = window.FlowCharts[uid];
    if(chart)
        chart.dispose();
}

class FFChart {
    uid;
    chartUid;
    data;
    url;
    seriesName;
    chart;
    chartBottomPad = 18;


    constructor(uid, args, dontGetData) {
        this.uid = uid;
        this.chartUid = uid + '-chart';
        
        this.url = args.url;
        this.seriesName = args.title;

        this.ele = document.getElementById(uid);
        this.ele.classList.add('chart-' + args.type);
        this.ele.addEventListener('dashboardElementResized', (event) => this.dashboardElementResized(event));
    
        if(dontGetData !== true)
            this.getData();
    }
    getHeight() {
        let chartDiv = this.ele.querySelector('.content');
        return chartDiv.clientHeight - this.chartBottomPad;
    }

    dashboardElementResized(event) {
        let height = this.getHeight();

        this.chart.updateOptions({
            chart: {
                height: height
            }
        }, true, false);
    }


    async getData() {
        if(this.disposed)
            return;

        let response = await fetch(this.url);
        let data = await response.json();
        data = this.fixData(data);

        if(this.hasData(data) === false){
            //document.getElementById(this.uid).style.display = 'none';
            return;
        }
        this.createChart(data);
    }
    
    hasData(data) {
        return data?.length;
    }
    
    fixData(data) {
        return data;
    }
    
    getChartOptions(data) {
        return {};
    }
    
    createChart(data, count){
        let height = this.getHeight();
        if(height < 0)
        {
            if(count > 5)
                return;
            setTimeout(() => this.createChart(data, count + 1), 50);
            return;
        }
        
        let defaultOptions = {
            chart: {              
                background: 'transparent',
                height: height,
                zoom: {
                    enabled: false
                },
                toolbar: {
                    show: false
                }
            },
            theme: {
                mode: 'dark'
            },
            stroke: {
                colors: ['#ffffff']
            },
            grid: {
                borderColor: '#90A4AE33'
            }
        };
        let instanceOptions = this.getChartOptions(data);
        let options = this.mergeDeep(defaultOptions, instanceOptions);
               
        let ele = document.getElementById(this.chartUid);
        if(ele) {
            this.chart = new ApexCharts(ele, options);
            this.chart.render();
        }
    }
    isObject(item) {
        return (item && typeof item === 'object' && !Array.isArray(item));
    }
    mergeDeep(target, ...sources) {
        if (!sources.length) return target;
        const source = sources.shift();

        if (this.isObject(target) && this.isObject(source)) {
            for (const key in source) {
                if (this.isObject(source[key])) {
                    if (!target[key]) Object.assign(target, { [key]: {} });
                    this.mergeDeep(target[key], source[key]);
                } else {
                    Object.assign(target, { [key]: source[key] });
                }
            }
        }

        return this.mergeDeep(target, ...sources);
    }

    dispose() {
        this.disposed = true;
        this.ele.removeEventListener('dashboardElementResized', dashboardElementResized);
    }
}



export class BoxPlotChart extends FFChart
{           
    constructor(uid, args) {
        super(uid, args);
    }

    getChartOptions(data){
        return {
            chart: {
                type: 'boxPlot',
            },
            plotOptions: {
                boxPlot: {
                    colors: {
                        upper: '#ff0090',
                        lower: '#84004bd9'
                    }
                }
            },
            series: [{
                data:data
            }]
        };
    }
}



export class HeatMapChart extends FFChart
{
    constructor(uid, args) {
        super(uid, args);
        this.chartBottomPad = 0;
    }

    getChartOptions(data){
        return {
            series: data,
            chart: {
                type: 'heatmap',
            },
            theme: {
                palette: 'palette6'
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
    }
}


export class PieChartChart extends FFChart
{
    constructor(uid, args) {
        super(uid, args);
    }

    hasData(data) {
        return !!data?.series?.length;
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
        return data;
    }


    getChartOptions(data)
    {
        return {
            chart: {
                type: 'donut',
            },
            theme: {
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
    }
}

export class TreeMapChart extends FFChart 
{
    constructor(uid, args) {
        super(uid, args);
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

    getChartOptions(data)
    {
        return {
            chart: {
                type: 'treemap',
            },
            colors: ['#33b2df'],
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
    }
}



export class TimeSeriesChart extends FFChart
{
    bottomUid;
    topUid;
    chartBottom;
    sizeData;
    countData;
    data;
    buckets;
    url;
    lastFetch;
    timer;
    maxValue;

    selectedRange = {
        start: null,
        end: null
    };

    constructor(uid, args) {
        super(uid, args, true);
        
        let options = args.flags !== null ? args.flags : this.ele.getAttribute('x-options');        

        this.bottomUid = uid + '-bottom';
        this.topUid = uid + '-top';
        this.sizeData = options === '1' === true;
        this.countData  = options === '2' === true;
        this.url = args.url;
        
        this.getData();
    }
    
    getTopHeight(){
        let height = this.getHeight();
        return height - this.getBottomHeight();
    }
    
    getBottomHeight(){
        let height = this.getHeight();
        return height > 200 ? 50 : 30;
    }

    async getData() {
        if(this.disposed)
            return;

        let data;
        if(this.lastFetch) {
            let time = new Date(this.lastFetch.getTime() + 1000);
            let fullDate = time.getFullYear() + '-' + ((time.getMonth() + 1).toString()).padStart(2, '0') + '-' + (time.getDate().toString()).padStart(2, '0')
            let fullTime = time.getHours().toString().padStart(2, '0') + ':' + time.getMinutes().toString().padStart(2, '0') + ':' + time.getSeconds().toString().padStart(2, '0')
                + '.' + time.getMilliseconds().toString().padEnd(3, '0');
            let response = await fetch(`${this.url}?since=${fullDate}T${fullTime}Z`);
            data = await response.json();
        }else {
            let response = await fetch(this.url);
            data = await response.json();
        }

        let max = 0;
        for(let d of data){
            if(typeof(d.x) === 'string')
                d.x = new Date(Date.parse(d.x));
            if(d.y === 0)
                d.y = 0.001; // just show it appears
            if(d.y > max)
                max = d.y;
        }
        this.maxValue = max;

        if(this.lastFetch)
            this.data = this.data.concat(data);
        else {
            this.data = data;
        }
        
        
        if(this.data.length > 0) 
        {
            this.lastFetch = this.data[this.data.length - 1].x;

            let buckets = this.adjustData(this.data, 100);
            let showBottom = buckets.length !== this.data.length;
            if (showBottom) {
                if (this.chartBottom)
                    this.updateBottom(buckets);
                else
                    this.buckets = buckets;
            } else {
                this.selectedRange.start = data[0].x;
                this.selectedRange.end = data[data.length - 1].x;
            }

            if (!this.chartTop)
                this.createTop();
            if (!this.chartBottom && showBottom)
                this.createBottom();

        }

        if(this.timer)
            clearTimeout(this.timer);
        if(!this.disposed)
            this.timer = setTimeout(() => this.getData(), 10000);
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

    updateBottom(buckets)
    {
        let oldEnd = this.buckets[this.buckets.length - 1].x;
        let newEnd = buckets[buckets.length - 1].x;

        let diff = newEnd.getTime() - oldEnd.getTime();
        this.buckets = buckets;

        this.chartBottom.updateSeries([{
            name: this.seriesName,
            data: this.buckets
        }]);

        this.selectedRange.start = new Date(this.selectedRange.start.getTime() + diff);
        this.selectedRange.end  = new Date(this.selectedRange.end.getTime() + diff);

        this.chartBottom.updateOptions(
            {
                chart: {
                    selection: {

                        xaxis: {
                            min: this.selectedRange.start.getTime(),
                            max: this.selectedRange.end.getTime()
                        }
                    }
                }
            }
        );
    }

    createTop(){
        let data = this.adjustData(this.data, 500);
        var options = {
            chart: {
                id: this.topUid,
                height: this.getTopHeight(),
                type: "area",
                background: 'transparent',
                toolbar: {
                    autoSelected: 'pan',
                    show:false
                },
                sparkline: {
                    enabled: true
                },
                animations: {
                    enabled: false
                },
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
                    name: this.seriesName,
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
                min:0,
                max: this.maxValue === 0.001 ? 1 : this.maxValue
            },
            markers: {
                colors: ["#00BAEC"],
                strokeColors: "#00BAEC",
                strokeWidth: 3
            },
            tooltip: {
                x: {
                    show:true,
                    formatter: (value, opts) => new Date(value).toLocaleTimeString()
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
                            if(this.countData)
                                return value === 0.001 ? '0' : value.toString();
                            return value.toFixed(1) + ' %';
                        }
                }
            }
        };
        
        let eleTop = document.getElementById(this.topUid);
        if(eleTop) {
            this.chartTop = new ApexCharts(eleTop, options);
            this.chartTop.render();
        }
    }

    updateTopTimeout;

    updateTopSelection(minDate, maxDate, dontWait)
    {
        this.selectedRange.start = minDate;
        this.selectedRange.end = maxDate;
        let doIt = () => {
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
        };

        if(dontWait)
            doIt();
        if(this.updateTopTimeout)
            clearTimeout(this.updateTopTimeout);
        this.updateTopTimeout = setTimeout(() => doIt(), 250);
    }


    createBottom(){
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
        if(yMax === 0.001)
            yMax = 1;
        this.selectedRange.start = brushStart;
        this.selectedRange.end = brushEnd;

        var options = {
            chart: {
                height: this.getBottomHeight(),
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
                yaxis: {
                    min:0
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
                    name: this.seriesName,
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

        let ele = document.getElementById(this.bottomUid);
        if(ele) {
            this.chartBottom = new ApexCharts(ele, options);
            this.chartBottom.render();
        }
    }


    dashboardElementResized(event) {
        let height = this.getTopHeight();

        this.chartTop.updateOptions({
            chart: {
                height: height
            }
        }, true, false);
    }

}