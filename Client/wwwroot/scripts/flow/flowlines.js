class ffFlowLines {
    ioNode = null;
    ioSelected = null;
    ioIsInput = null;
    ioContext = null;
    ioSourceBounds = null;
    ioCanvasBounds = null;
    ioOutputConnections = new Map();
    ioLines = [];

    ioCanvas = null;
    ioSelectedConnection = null;
    accentColor = null;
    lineColor = null;
    ioOffset = 6;

    reset() {
        this.ioNode = null;
        this.ioSelected = null;
        this.ioIsInput = null;
        this.ioContext = null;
        this.ioSourceBounds = null;
        this.ioCanvasBounds = null;
        this.ioLines = [];
        this.ioOutputConnections = new Map();
        this.ioCanvas = null;
        this.ioSelectedConnection = null;
    }

    ioDown(event, isInput) {
        this.ioNode = event.target;
        this.ioSelected = this.ioNode.parentNode.parentNode;
        this.ioIsInput = isInput;

        let canvas = document.querySelector('canvas');
        this.ioCanvasBounds = canvas.getBoundingClientRect();
        var srcBounds = this.ioNode.getBoundingClientRect();
        let srcX = (srcBounds.left - this.ioCanvasBounds.left) + this.ioOffset;
        let srcY = (srcBounds.top - this.ioCanvasBounds.top) + this.ioOffset;
        this.ioSourceBounds = { left: srcX, top: srcY };

        if (this.selectedOutput != null) {
        }
        else {
            // start drawing line
        }
    };

    ioMouseMove(event) {
        if (!this.ioNode)
            return;

        let destX = event.clientX - this.ioCanvasBounds.left;
        let destY = event.clientY - this.ioCanvasBounds.top;
        this.redrawLines();
        this.drawLineToPoint(this.ioSourceBounds.left, this.ioSourceBounds.top, destX, destY);
    };

    ioMouseUp(event) {
        if (!this.ioNode)
            return;

        let target = event.target;
        let suitable = target?.classList?.contains(this.ioIsInput ? 'output' : 'input') === true;
        if (suitable) {
            let input = this.isInput ? this.ioNode : target;
            let output = this.isInput ? target : this.ioNode;
            let outputId = output.getAttribute('id');

            if (input.classList.contains('connected') === false)
                input.classList.add('connected');
            if (output.classList.contains('connected') === false)
                output.classList.add('connected');

            let connections = this.ioOutputConnections.get(outputId);
            if (!connections) {
                this.ioOutputConnections.set(outputId, []);
                connections = this.ioOutputConnections.get(outputId);
            }
            let index = parseInt(input.getAttribute('x-input'), 10);

            let part = input.parentNode.parentNode.getAttribute('id');
            let existing = connections.filter(x => x.index == index && x.part == part);
            if (!existing || existing.length === 0) {


                if (ffFlow.SingleOutputConnection) {
                    connections = [{ index: index, part: part }];
                    this.ioOutputConnections.set(outputId, connections);
                }
                else
                    connections.push({ index: index, part: part });

                this.drawLine(input, output);
            }
        }

        this.ioNode = null;
        this.ioSelected = null;
        this.redrawLines();
    };

    redrawLines() {
        this.ioLines = [];
        let outputs = document.querySelectorAll('.flow-part .output');
        let canvas = this.getCanvas();
        if (!this.ioContext) {
            this.ioContext = canvas.getContext('2d');
        }
        this.ioContext.clearRect(0, 0, canvas.width, canvas.height);

        for (let output of outputs) {
            let connections = this.ioOutputConnections.get(output.getAttribute('id'));
            if (!connections)
                continue;
            for (let input of connections) {
                let inputEle = document.getElementById(input.part + '-input-' + input.index);
                if (!inputEle)
                    console.log('failed to locate: ' + input.part + '-input-' + input.index, input);
                this.drawLine(inputEle, output, input);
            }
        }
    };

    drawLine(input, output, connection) {
        if (!input || !output)
            return;

        let src = output;
        let dest = input;
        let srcBounds = src.getBoundingClientRect();
        let destBounds = dest.getBoundingClientRect();

        let canvas = this.getCanvas();
        let canvasBounds = canvas.getBoundingClientRect();
        let srcX = (srcBounds.left - canvasBounds.left) + this.ioOffset;
        let srcY = (srcBounds.top - canvasBounds.top) + this.ioOffset;
        let destX = (destBounds.left - canvasBounds.left) + this.ioOffset;
        let destY = (destBounds.top - canvasBounds.top) + this.ioOffset;
        this.drawLineToPoint(srcX, srcY, destX, destY, output, connection);
    };

    drawLineToPoint(srcX, srcY, destX, destY, output, connection) {
        if (!this.ioContext) {
            let canvas = this.getCanvas();
            this.ioContext = canvas.getContext('2d');
        }

        const context = this.ioContext;

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
        context.strokeStyle = this.lineColor;
        //context.fill(path);
        context.stroke(path);

        this.ioLines.push({ path: path, output: output, connection: connection });

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
        // if(!this.accentColor)
        //     this.accentColor = this.colorFromCssClass('--accent');
        // context.strokeStyle = this.accentColor;
        // context.stroke();
    };

    getCanvas() {
        if (this.ioCanvas)
            return this.ioCanvas;

        this.accentColor = this.colorFromCssClass('--accent');
        this.lineColor = this.colorFromCssClass('--color-darkest');

        let canvas = document.querySelector('canvas');
        // Listen for mouse moves
        let self = this;
        canvas.addEventListener('mousedown', function (event) {
            let ctx = self.ioContext;
            self.ioSelectedConnection = null;
            for (let line of self.ioLines) {
                // Check whether point is inside ellipse's stroke
                if (ctx.isPointInStroke(line.path, event.offsetX, event.offsetY)) {
                    ctx.strokeStyle = self.accentColor;
                    self.ioSelectedConnection = line;
                }
                else {
                    ctx.strokeStyle = self.lineColor;
                }

                // Draw ellipse
                ctx.stroke(line.path);
            }
        });
        canvas.addEventListener('keydown', function (event) {
            if (event.code === 'Delete')
                self.deleteConnection();
        });
        this.ioCanvas = canvas;
        return this.ioCanvas;
    }

    deleteConnection() {
        if (!this.ioSelectedConnection)
            return;

        let selected = this.ioSelectedConnection;
        let outputNodeUid = selected.output.getAttribute('id');
        let connections = this.ioOutputConnections.get(outputNodeUid);
        let index = connections.indexOf(selected.connection);
        if (index >= 0) {
            connections.splice(index, 1);
        }

        this.redrawLines();
    }


    colorFromCssClass(variable) {
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
    }
}