window.ViFlow = {
    active: false,
    currentX: 0,
    currentY: 0,
    initialX: 0,
    initialY: 0,
    xOffset: 0,
    yOffset: 0,
    dragItem: null,
    csharp: null,
    draggingElementUid: null,

    init: function (container, csharp) {
        ViFlow.csharp = csharp;
        if (typeof (container) === 'string') {
            let c = document.getElementById(container);
            if (!c)
                c = document.querySelector(container);
            if (!c) {
                console.warn("Failed to locate container:", container);
                return;
            }
            container = c;
        }
        container.addEventListener("touchstart", ViFlow.dragStart, false);
        container.addEventListener("touchend", ViFlow.dragEnd, false);
        container.addEventListener("touchmove", ViFlow.drag, false);

        container.addEventListener("mousedown", ViFlow.dragStart, false);
        container.addEventListener("mouseup", ViFlow.dragEnd, false);
        container.addEventListener("mouseup", ViFlow.ioMouseUp, false);
        container.addEventListener("mousemove", ViFlow.drag, false);
        container.addEventListener("mousemove", ViFlow.ioMouseMove, false);
        


        let canvas = document.querySelector('canvas');
        canvas.height = window.screen.availHeight;
        canvas.width = window.screen.availWidth;
        canvas.style.width = canvas.width + 'px';
        canvas.style.height = canvas.height + 'px';

    },

    dragStart: function (e) {
        if (e.type === "touchstart") {
            ViFlow.initialX = e.touches[0].clientX - ViFlow.xOffset;
            ViFlow.initialY = e.touches[0].clientY - ViFlow.yOffset;
        } else {
            ViFlow.initialX = e.clientX;// - ViFlow.xOffset;
            ViFlow.initialY = e.clientY;// - ViFlow.yOffset;
        }


        if (e.target.classList.contains('draggable') === true) {
            ViFlow.currentX = 0;
            ViFlow.currentY = 0;
            ViFlow.dragItem = e.target.parentNode;
            ViFlow.active = true;

        }
    },

    dragEnd: function (e) {
        ViFlow.initialX = ViFlow.currentX;
        ViFlow.initialY = ViFlow.currentY;
        if (ViFlow.active && ViFlow.dragItem) {
            ViFlow.dragItem.style.transform = '';
            let xPos = parseInt(ViFlow.dragItem.style.left, 10) + ViFlow.currentX;
            let yPos = parseInt(ViFlow.dragItem.style.top, 10) + ViFlow.currentY;
            ViFlow.dragItem.style.left = xPos + 'px';
            ViFlow.dragItem.style.top = yPos + 'px';

            ViFlow.csharp.invokeMethodAsync("UpdatePosition", ViFlow.dragItem.getAttribute('id'), xPos, yPos);

            ViFlow.redrawLines();
        }
        ViFlow.active = false;
    },

    drag: function (e) {
        if (ViFlow.active) {

            e.preventDefault();

            if (e.type === "touchmove") {
                ViFlow.currentX = e.touches[0].clientX - ViFlow.initialX;
                ViFlow.currentY = e.touches[0].clientY - ViFlow.initialY;
            } else {
                ViFlow.currentX = e.clientX - ViFlow.initialX;
                ViFlow.currentY = e.clientY - ViFlow.initialY;
            }


            ViFlow.xOffset = ViFlow.currentX;
            ViFlow.yOffset = ViFlow.currentY;

            ViFlow.setTranslate(ViFlow.currentX, ViFlow.currentY, ViFlow.dragItem);
            ViFlow.redrawLines();
        }
    },

    setTranslate: function (xPos, yPos, el) {
        el.style.transform = "translate3d(" + xPos + "px, " + yPos + "px, 0)";
    },






    dragElementStart: function (event) {
        ViFlow.draggingElementUid = event.target.id;
        event.dataTransfer.setData("text", event.target.id);
        event.dataTransfer.effectAllowed = "copy";
    },

    drop: function (event, dropping) {
        event.preventDefault();
        if (dropping !== true)
            return;
        //console.log('dropped', ViFlow.draggingElementUid);
        let bounds = event.target.getBoundingClientRect();
//        console.log('target', event.target, event.target.offsetLeft, event.target.offsetTop, bounds);

        let xPos = event.clientX - bounds.left - 20;
        let yPos = event.clientY - bounds.top - 20;

        ViFlow.csharp.invokeMethodAsync("AddElement", ViFlow.draggingElementUid, xPos, yPos);        
    },




    ioNode: null,
    ioSelected: null,
    ioIsInput: null,
    ioContext: null,
    ioCanvas: null,
    ioSourceBounds: null,
    ioCanvasBounds: null,
    ioOutputConnections: new Map(),
    ioDown: function (event, isInput) {
        ViFlow.ioNode = event.target;
        ViFlow.ioSelected = ViFlow.ioNode.parentNode.parentNode;
        ViFlow.ioIsInput = isInput;

        let canvas = document.querySelector('canvas');
        ViFlow.ioCanvasBounds = canvas.getBoundingClientRect();     
        var srcBounds = ViFlow.ioNode.getBoundingClientRect();   
        let srcX = srcBounds.left - ViFlow.ioCanvasBounds.left;
        let srcY = srcBounds.top - ViFlow.ioCanvasBounds.top;
        ViFlow.ioSourceBounds = { left: srcX, top: srcY };
        
        if (ViFlow.selectedOutput != null) {
        }
        else {
            // start drawing line
        }
    },

    ioMouseMove: function (event) {
        if (!ViFlow.ioNode)
            return;
        
        let destX = event.clientX - ViFlow.ioCanvasBounds.left;
        let destY = event.clientY - ViFlow.ioCanvasBounds.top;
        ViFlow.redrawLines();
        ViFlow.drawLineToPoint(ViFlow.ioSourceBounds.left, ViFlow.ioSourceBounds.top, destX, destY);        
    },

    ioMouseUp: function (event) {
        if (!ViFlow.ioNode)
            return;
                
        console.log('ioMouseUp', event.target);
        let target = event.target;
        let suitable = target?.classList?.contains(ViFlow.ioIsInput ? 'output' : 'input') === true;
        if (suitable) {
            console.log('suitable drop target!');
            let input = ViFlow.isInput ? ViFlow.ioNode : target;
            let output = ViFlow.isInput ? target : ViFlow.ioNode;

            console.log('input', input, 'output', output);
            ViFlow.drawLine(input, output);

            if (!ViFlow.ioOutputConnections[output])
                ViFlow.ioOutputConnections.set(output, []);
            ViFlow.ioOutputConnections.get(output).push(input);
            console.log('ouptut connections', ViFlow.ioOutputConnections[output]);
        }
        
        ViFlow.ioNode = null;
        ViFlow.ioSelected = null;
        ViFlow.redrawLines();
    },


    colorFromCssClass: function (variable) {
        var tmp = document.createElement("div"), color;
        tmp.style.cssText = "position:fixed;left:-100px;top:-100px;width:1px;height:1px";
        tmp.style.color = "var(" + variable + ")";
        document.body.appendChild(tmp);  // required in some browsers
        color = getComputedStyle(tmp).getPropertyValue("color");
        document.body.removeChild(tmp);
        return color;
    },

    redrawLines: function () {
        let outputs = document.querySelectorAll('.flow-part .output');
        let canvas = document.querySelector('canvas');
        if (!ViFlow.ioContext)
            return;
        ViFlow.ioContext.clearRect(0, 0, canvas.width, canvas.height);

        for (let output of outputs) {
            let connections = ViFlow.ioOutputConnections.get(output);
            if(!connections)
                continue;
            for (let input of connections) {
                ViFlow.drawLine(input, output);
            }
        }
    },

    accentColor: null,

    drawLine: function (input, output) {
        
        let src = output;
        let dest = input;
        let srcBounds = src.getBoundingClientRect();
        let destBounds = dest.getBoundingClientRect();

        if (!ViFlow.ioCanvas)
            ViFlow.ioCanvas = document.querySelector('canvas');
        let canvasBounds = ViFlow.ioCanvas.getBoundingClientRect();

        let srcX = srcBounds.left - canvasBounds.left;
        let srcY = srcBounds.top - canvasBounds.top;
        let destX = destBounds.left - canvasBounds.left;
        let destY = destBounds.top - canvasBounds.top;
        ViFlow.drawLineToPoint(srcX, srcY, destX, destY);
    },

    drawLineToPoint: function ( srcX, srcY, destX, destY)
    {
        if (!ViFlow.ioContext) {
            if (!ViFlow.ioCanvas)
                ViFlow.ioCanvas = document.querySelector('canvas');
            ViFlow.ioContext = ViFlow.ioCanvas.getContext('2d');
        }
        
        ViFlow.ioContext.beginPath();

        ViFlow.ioContext.moveTo(srcX, srcY);
        ViFlow.ioContext.lineTo(destX, destY);
        ViFlow.ioContext.lineWidth = 5;
        if(!ViFlow.accentColor)
            ViFlow.accentColor = ViFlow.colorFromCssClass('--accent');
        ViFlow.ioContext.strokeStyle = ViFlow.accentColor;
        ViFlow.ioContext.stroke();
    }
}