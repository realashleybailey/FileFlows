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

    reset: function () {
        ViFlow.active = false;
        ViFlow.currentX = 0;
        ViFlow.currentY = 0;
        ViFlow.initialX = 0;
        ViFlow.initialY = 0;
        ViFlow.xOffset = 0;
        ViFlow.yOffset = 0;
        ViFlow.dragItem = null;
        ViFlow.csharp = null;
        ViFlow.draggingElementUid = null;
        ViFlow.ioCanvas = null;
        ViFlow.ioSelectedConnection = null;
        ViFlow.ioNode = null;
        ViFlow.ioSelected = null;
        ViFlow.ioIsInput = null;
        ViFlow.ioContext = null;
        ViFlow.ioSourceBounds = null;
        ViFlow.ioCanvasBounds = null;
        ViFlow.ioLines = [];
        ViFlow.ioOutputConnections = new Map();
    },

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

        ViFlow.redrawLines();
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
        let bounds = event.target.getBoundingClientRect();

        let xPos = event.clientX - bounds.left - 20;
        let yPos = event.clientY - bounds.top - 20;

        ViFlow.csharp.invokeMethodAsync("AddElement", ViFlow.draggingElementUid, xPos, yPos);
    },

    ioCanvas: null,
    ioSelectedConnection: null,
    accentColor: null,
    lineColor: null,
    getCanvas: function () {

        if (ViFlow.ioCanvas)
            return ViFlow.ioCanvas;

        ViFlow.accentColor = ViFlow.colorFromCssClass('--accent');
        ViFlow.lineColor = ViFlow.colorFromCssClass('--color-darkest');

        let canvas = document.querySelector('canvas');
        // Listen for mouse moves
        canvas.addEventListener('mousedown', function (event) {
            let ctx = ViFlow.ioContext;
            ViFlow.ioSelectedConnection = null;
            for (let line of ViFlow.ioLines) {
                // Check whether point is inside ellipse's stroke
                if (ctx.isPointInStroke(line.path, event.offsetX, event.offsetY)) {
                    ctx.strokeStyle = ViFlow.accentColor;
                    ViFlow.ioSelectedConnection = line;
                }
                else {
                    ctx.strokeStyle = ViFlow.lineColor;
                }

                // Draw ellipse
                ctx.stroke(line.path);
            }
        });
        canvas.addEventListener('keydown', function (event) {
            if (event.code === 'Delete' && ViFlow.ioSelectedConnection) {
                console.log('deleting');
                let selected = ViFlow.ioSelectedConnection;
                let outputUid = selected.output.parentNode.parentNode.getAttribute('id');
                let outputNodeUid = selected.output.getAttribute('id');
                let outputIndex = parseInt(selected.output.getAttribute('x-output'), 10);
                let connections = ViFlow.ioOutputConnections.get(outputNodeUid);
                let index = connections.indexOf(selected.connection);
                if (index >= 0) {
                    connections.splice(index, 1);
                }
                console.log('delete 2',
                    selected.connection.part,
                    outputUid,
                    selected.connection.index,
                    outputIndex);

                ViFlow.csharp.invokeMethodAsync("RemoveConnection",
                    selected.connection.part,
                    outputUid,
                    selected.connection.index,
                    outputIndex
                );

                ViFlow.redrawLines();
            }
        });
        ViFlow.ioCanvas = canvas;
        return ViFlow.ioCanvas;
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
        ViFlow.reset();
        for (let k in connections) { // iterating keys so use in
            for (let con of connections[k]) { // iterating values so use of
                let id = k + '-output-' + con.output;

                let list = ViFlow.ioOutputConnections.get(id);
                if (!list) {
                    ViFlow.ioOutputConnections.set(id, []);
                    list = ViFlow.ioOutputConnections.get(id);
                }
                list.push({ index: con.input, part: con.inputNode });
            }

        }

        // ViFlow.ioOutputConnections.set(output, []);
        //     ViFlow.ioOutputConnections.get(output).push(input);
    },
    ioDown: function (event, isInput) {
        ViFlow.ioNode = event.target;
        ViFlow.ioSelected = ViFlow.ioNode.parentNode.parentNode;
        ViFlow.ioIsInput = isInput;

        let canvas = document.querySelector('canvas');
        ViFlow.ioCanvasBounds = canvas.getBoundingClientRect();
        var srcBounds = ViFlow.ioNode.getBoundingClientRect();
        let srcX = (srcBounds.left - ViFlow.ioCanvasBounds.left) + ViFlow.ioOffset;
        let srcY = (srcBounds.top - ViFlow.ioCanvasBounds.top) + ViFlow.ioOffset;
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

        let target = event.target;
        let suitable = target?.classList?.contains(ViFlow.ioIsInput ? 'output' : 'input') === true;
        if (suitable) {
            let input = ViFlow.isInput ? ViFlow.ioNode : target;
            let output = ViFlow.isInput ? target : ViFlow.ioNode;
            let outputId = output.getAttribute('id');

            if (input.classList.contains('connected') === false)
                input.classList.add('connected');
            if (output.classList.contains('connected') === false)
                output.classList.add('connected');

            let connections = ViFlow.ioOutputConnections.get(outputId);
            if (!connections) {
                ViFlow.ioOutputConnections.set(outputId, []);
                connections = ViFlow.ioOutputConnections.get(outputId);
            }
            let index = parseInt(input.getAttribute('x-input'), 10);

            let part = input.parentNode.parentNode.getAttribute('id');
            let existing = connections.filter(x => x.index == index && x.part == part);
            if (!existing || existing.length === 0) {

                ViFlow.drawLine(input, output);

                connections.push({ index: index, part: part });

                ViFlow.csharp.invokeMethodAsync("AddConnection",
                    input.parentNode.parentNode.getAttribute('id'),
                    output.parentNode.parentNode.getAttribute('id'),
                    parseInt(input.getAttribute("x-input"), 10),
                    parseInt(output.getAttribute("x-output"), 10)
                );
            }
        }

        ViFlow.ioNode = null;
        ViFlow.ioSelected = null;
        ViFlow.redrawLines();
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
        ViFlow.ioLines = [];
        let outputs = document.querySelectorAll('.flow-part .output');
        let canvas = ViFlow.getCanvas();
        if (!ViFlow.ioContext) {
            ViFlow.ioContext = canvas.getContext('2d');
        }
        ViFlow.ioContext.clearRect(0, 0, canvas.width, canvas.height);

        for (let output of outputs) {
            let connections = ViFlow.ioOutputConnections.get(output.getAttribute('id'));
            if (!connections)
                continue;
            for (let input of connections) {
                let inputEle = document.getElementById(input.part + '-input-' + input.index);
                if (!inputEle)
                    console.log('failed to locate: ' + input.part + '-input-' + input.index, input);
                ViFlow.drawLine(inputEle, output, input);
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

        let canvas = ViFlow.getCanvas();
        let canvasBounds = canvas.getBoundingClientRect();

        let srcX = (srcBounds.left - canvasBounds.left) + ViFlow.ioOffset;
        let srcY = (srcBounds.top - canvasBounds.top) + ViFlow.ioOffset;
        let destX = (destBounds.left - canvasBounds.left) + ViFlow.ioOffset;
        let destY = (destBounds.top - canvasBounds.top) + ViFlow.ioOffset;
        ViFlow.drawLineToPoint(srcX, srcY, destX, destY, output, connection);
    },

    drawLineToPoint: function (srcX, srcY, destX, destY, output, connection) {
        if (!ViFlow.ioContext) {
            let canvas = ViFlow.getCanvas();
            ViFlow.ioContext = canvas.getContext('2d');
        }

        const context = ViFlow.ioContext;


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
        context.strokeStyle = ViFlow.lineColor;
        //context.fill(path);
        context.stroke(path);

        ViFlow.ioLines.push({ path: path, output: output, connection: connection });

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
        // if(!ViFlow.accentColor)
        //     ViFlow.accentColor = ViFlow.colorFromCssClass('--accent');
        // context.strokeStyle = ViFlow.accentColor;
        // context.stroke();
    },


    getModel: function () {
        var parts = document.querySelectorAll(".flow-parts .flow-part");
    }
}