window.ffFlow = {
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

    reset: function () {
        ffFlow.active = false;
        ffFlow.currentX = 0;
        ffFlow.currentY = 0;
        ffFlow.initialX = 0;
        ffFlow.initialY = 0;
        ffFlow.xOffset = 0;
        ffFlow.yOffset = 0;
        ffFlow.dragItem = null;
        ffFlow.csharp = null;
        ffFlow.draggingElementUid = null;
        ffFlow.ioCanvas = null;
        ffFlow.ioSelectedConnection = null;
        ffFlow.ioNode = null;
        ffFlow.ioSelected = null;
        ffFlow.ioIsInput = null;
        ffFlow.ioContext = null;
        ffFlow.ioSourceBounds = null;
        ffFlow.ioCanvasBounds = null;
        ffFlow.ioLines = [];
        ffFlow.ioOutputConnections = new Map();
    },

    init: function (container, csharp) {
        ffFlow.csharp = csharp;
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
        container.addEventListener("touchstart", ffFlow.dragStart, false);
        container.addEventListener("touchend", ffFlow.dragEnd, false);
        container.addEventListener("touchmove", ffFlow.drag, false);

        container.addEventListener("mousedown", ffFlow.dragStart, false);
        container.addEventListener("mouseup", ffFlow.dragEnd, false);
        container.addEventListener("mouseup", ffFlow.ioMouseUp, false);
        container.addEventListener("mousemove", ffFlow.drag, false);
        container.addEventListener("mousemove", ffFlow.ioMouseMove, false);



        let canvas = document.querySelector('canvas');
        canvas.height = window.screen.availHeight;
        canvas.width = window.screen.availWidth;
        canvas.style.width = canvas.width + 'px';
        canvas.style.height = canvas.height + 'px';

        ffFlow.redrawLines();
    },

    dragStart: function (e) {
        if (e.type === "touchstart") {
            ffFlow.initialX = e.touches[0].clientX - ffFlow.xOffset;
            ffFlow.initialY = e.touches[0].clientY - ffFlow.yOffset;
        } else {
            ffFlow.initialX = e.clientX;// - ffFlow.xOffset;
            ffFlow.initialY = e.clientY;// - ffFlow.yOffset;
        }


        if (e.target.classList.contains('draggable') === true) {
            ffFlow.currentX = 0;
            ffFlow.currentY = 0;
            ffFlow.dragItem = e.target.parentNode;
            ffFlow.active = true;

        }
    },

    dragEnd: function (e) {
        ffFlow.initialX = ffFlow.currentX;
        ffFlow.initialY = ffFlow.currentY;
        if (ffFlow.active && ffFlow.dragItem) {
            ffFlow.dragItem.style.transform = '';
            let xPos = parseInt(ffFlow.dragItem.style.left, 10) + ffFlow.currentX;
            let yPos = parseInt(ffFlow.dragItem.style.top, 10) + ffFlow.currentY;
            ffFlow.dragItem.style.left = xPos + 'px';
            ffFlow.dragItem.style.top = yPos + 'px';

            ffFlow.csharp.invokeMethodAsync("UpdatePosition", ffFlow.dragItem.getAttribute('id'), xPos, yPos);

            ffFlow.redrawLines();
        }
        ffFlow.active = false;
    },

    drag: function (e) {
        if (ffFlow.active) {

            e.preventDefault();

            if (e.type === "touchmove") {
                ffFlow.currentX = e.touches[0].clientX - ffFlow.initialX;
                ffFlow.currentY = e.touches[0].clientY - ffFlow.initialY;
            } else {
                ffFlow.currentX = e.clientX - ffFlow.initialX;
                ffFlow.currentY = e.clientY - ffFlow.initialY;
            }


            ffFlow.xOffset = ffFlow.currentX;
            ffFlow.yOffset = ffFlow.currentY;

            ffFlow.setTranslate(ffFlow.currentX, ffFlow.currentY, ffFlow.dragItem);
            ffFlow.redrawLines();
        }
    },

    setTranslate: function (xPos, yPos, el) {
        el.style.transform = "translate3d(" + xPos + "px, " + yPos + "px, 0)";
    },






    dragElementStart: function (event) {
        ffFlow.draggingElementUid = event.target.id;
        event.dataTransfer.setData("text", event.target.id);
        event.dataTransfer.effectAllowed = "copy";
    },

    drop: function (event, dropping) {
        event.preventDefault();
        if (dropping !== true)
            return;
        let bounds = event.target.getBoundingClientRect();

        let xPos = event.clientX - bounds.left - 20;
        let yPos = event.clientY - bounds.top - 20;

        ffFlow.csharp.invokeMethodAsync("AddElement", ffFlow.draggingElementUid, xPos, yPos);
    },

    ioCanvas: null,
    ioSelectedConnection: null,
    accentColor: null,
    lineColor: null,
    getCanvas: function () {

        if (ffFlow.ioCanvas)
            return ffFlow.ioCanvas;

        ffFlow.accentColor = ffFlow.colorFromCssClass('--accent');
        ffFlow.lineColor = ffFlow.colorFromCssClass('--color-darkest');

        let canvas = document.querySelector('canvas');
        // Listen for mouse moves
        canvas.addEventListener('mousedown', function (event) {
            let ctx = ffFlow.ioContext;
            ffFlow.ioSelectedConnection = null;
            for (let line of ffFlow.ioLines) {
                // Check whether point is inside ellipse's stroke
                if (ctx.isPointInStroke(line.path, event.offsetX, event.offsetY)) {
                    ctx.strokeStyle = ffFlow.accentColor;
                    ffFlow.ioSelectedConnection = line;
                }
                else {
                    ctx.strokeStyle = ffFlow.lineColor;
                }

                // Draw ellipse
                ctx.stroke(line.path);
            }
        });
        canvas.addEventListener('keydown', function (event) {
            if (event.code === 'Delete' && ffFlow.ioSelectedConnection) {
                console.log('deleting');
                let selected = ffFlow.ioSelectedConnection;
                let outputUid = selected.output.parentNode.parentNode.getAttribute('id');
                let outputNodeUid = selected.output.getAttribute('id');
                let outputIndex = parseInt(selected.output.getAttribute('x-output'), 10);
                let connections = ffFlow.ioOutputConnections.get(outputNodeUid);
                let index = connections.indexOf(selected.connection);
                if (index >= 0) {
                    connections.splice(index, 1);
                }
                console.log('delete 2',
                    selected.connection.part,
                    outputUid,
                    selected.connection.index,
                    outputIndex);

                ffFlow.csharp.invokeMethodAsync("RemoveConnection",
                    selected.connection.part,
                    outputUid,
                    selected.connection.index,
                    outputIndex
                );

                ffFlow.redrawLines();
            }
        });
        ffFlow.ioCanvas = canvas;
        return ffFlow.ioCanvas;
    },



    ioNode: null,
    ioSelected: null,
    ioIsInput: null,
    ioContext: null,
    ioSourceBounds: null,
    ioCanvasBounds: null,
    ioOutputConnections: new Map(),
    ioLines: [],
    ioOffset: 6,
    ioInitConnections: function (connections) {
        ffFlow.reset();
        for (let k in connections) { // iterating keys so use in
            for (let con of connections[k]) { // iterating values so use of
                let id = k + '-output-' + con.output;

                let list = ffFlow.ioOutputConnections.get(id);
                if (!list) {
                    ffFlow.ioOutputConnections.set(id, []);
                    list = ffFlow.ioOutputConnections.get(id);
                }
                list.push({ index: con.input, part: con.inputNode });
            }

        }

        // ffFlow.ioOutputConnections.set(output, []);
        //     ffFlow.ioOutputConnections.get(output).push(input);
    },
    ioDown: function (event, isInput) {
        ffFlow.ioNode = event.target;
        ffFlow.ioSelected = ffFlow.ioNode.parentNode.parentNode;
        ffFlow.ioIsInput = isInput;

        let canvas = document.querySelector('canvas');
        ffFlow.ioCanvasBounds = canvas.getBoundingClientRect();
        var srcBounds = ffFlow.ioNode.getBoundingClientRect();
        let srcX = (srcBounds.left - ffFlow.ioCanvasBounds.left) + ffFlow.ioOffset;
        let srcY = (srcBounds.top - ffFlow.ioCanvasBounds.top) + ffFlow.ioOffset;
        ffFlow.ioSourceBounds = { left: srcX, top: srcY };

        if (ffFlow.selectedOutput != null) {
        }
        else {
            // start drawing line
        }
    },

    ioMouseMove: function (event) {
        if (!ffFlow.ioNode)
            return;

        let destX = event.clientX - ffFlow.ioCanvasBounds.left;
        let destY = event.clientY - ffFlow.ioCanvasBounds.top;
        ffFlow.redrawLines();
        ffFlow.drawLineToPoint(ffFlow.ioSourceBounds.left, ffFlow.ioSourceBounds.top, destX, destY);
    },

    ioMouseUp: function (event) {
        if (!ffFlow.ioNode)
            return;

        let target = event.target;
        let suitable = target?.classList?.contains(ffFlow.ioIsInput ? 'output' : 'input') === true;
        if (suitable) {
            let input = ffFlow.isInput ? ffFlow.ioNode : target;
            let output = ffFlow.isInput ? target : ffFlow.ioNode;
            let outputId = output.getAttribute('id');

            if (input.classList.contains('connected') === false)
                input.classList.add('connected');
            if (output.classList.contains('connected') === false)
                output.classList.add('connected');

            let connections = ffFlow.ioOutputConnections.get(outputId);
            if (!connections) {
                ffFlow.ioOutputConnections.set(outputId, []);
                connections = ffFlow.ioOutputConnections.get(outputId);
            }
            let index = parseInt(input.getAttribute('x-input'), 10);

            let part = input.parentNode.parentNode.getAttribute('id');
            let existing = connections.filter(x => x.index == index && x.part == part);
            if (!existing || existing.length === 0) {

                ffFlow.drawLine(input, output);

                connections.push({ index: index, part: part });

                ffFlow.csharp.invokeMethodAsync("AddConnection",
                    input.parentNode.parentNode.getAttribute('id'),
                    output.parentNode.parentNode.getAttribute('id'),
                    parseInt(input.getAttribute("x-input"), 10),
                    parseInt(output.getAttribute("x-output"), 10)
                );
            }
        }

        ffFlow.ioNode = null;
        ffFlow.ioSelected = null;
        ffFlow.redrawLines();
    },


    colorFromCssClass: function (variable) {
        var tmp = document.createElement("div"), color;
        tmp.style.cssText = "position:fixed;left:-100px;top:-100px;width:1px;height:1px";
        if (variable.startsWith('--'))
            tmp.style.color = "var(" + variable + ")";
        else
            tmp.style.color = variable;
        document.body.appendChild(tmp);  // required in some browsers
        color = getComputedStyle(tmp).getPropertyValue("color");
        document.body.removeChild(tmp);
        return color;
    },

    redrawLines: function () {
        ffFlow.ioLines = [];
        let outputs = document.querySelectorAll('.flow-part .output');
        let canvas = ffFlow.getCanvas();
        if (!ffFlow.ioContext) {
            ffFlow.ioContext = canvas.getContext('2d');
        }
        ffFlow.ioContext.clearRect(0, 0, canvas.width, canvas.height);

        for (let output of outputs) {
            let connections = ffFlow.ioOutputConnections.get(output.getAttribute('id'));
            if (!connections)
                continue;
            for (let input of connections) {
                let inputEle = document.getElementById(input.part + '-input-' + input.index);
                if (!inputEle)
                    console.log('failed to locate: ' + input.part + '-input-' + input.index, input);
                ffFlow.drawLine(inputEle, output, input);
            }
        }
    },

    drawLine: function (input, output, connection) {

        if (!input || !output)
            return;

        let src = output;
        let dest = input;
        let srcBounds = src.getBoundingClientRect();
        let destBounds = dest.getBoundingClientRect();

        let canvas = ffFlow.getCanvas();
        let canvasBounds = canvas.getBoundingClientRect();

        let srcX = (srcBounds.left - canvasBounds.left) + ffFlow.ioOffset;
        let srcY = (srcBounds.top - canvasBounds.top) + ffFlow.ioOffset;
        let destX = (destBounds.left - canvasBounds.left) + ffFlow.ioOffset;
        let destY = (destBounds.top - canvasBounds.top) + ffFlow.ioOffset;
        ffFlow.drawLineToPoint(srcX, srcY, destX, destY, output, connection);
    },

    drawLineToPoint: function (srcX, srcY, destX, destY, output, connection) {
        if (!ffFlow.ioContext) {
            let canvas = ffFlow.getCanvas();
            ffFlow.ioContext = canvas.getContext('2d');
        }

        const context = ffFlow.ioContext;


        const path = new Path2D();
        path.moveTo(srcX, srcY);
        if (Math.abs(destY - srcY) <= 50) {
            path.lineTo(destX, destY);
        } else {
            path.bezierCurveTo(srcX + 50, srcY + 50,
                destX - 50, destY - 50,
                destX, destY);
        }

        context.lineWidth = 5;
        context.strokeStyle = ffFlow.lineColor;
        //context.fill(path);
        context.stroke(path);

        ffFlow.ioLines.push({ path: path, output: output, connection: connection });

        //context.beginPath();
        // context.moveTo(srcX, srcY);
        // if (Math.abs(destY - srcY) <= 50) {
        //     context.lineTo(destX, destY);
        // } else {
        //     context.bezierCurveTo(srcX + 50, srcY + 50,
        //         destX - 50, destY - 50,
        //         destX, destY);
        // }
        // context.lineWidth = 5;
        // if(!ffFlow.accentColor)
        //     ffFlow.accentColor = ffFlow.colorFromCssClass('--accent');
        // context.strokeStyle = ffFlow.accentColor;
        // context.stroke();
    },


    getModel: function () {
        var parts = document.querySelectorAll(".flow-parts .flow-part");
    }
}