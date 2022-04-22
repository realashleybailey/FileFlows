window.ffFlow = {
    active: false,
    csharp: null,
    parts: [],
    elements: [],
    FlowLines: new ffFlowLines(),
    Mouse: new ffFlowMouse(),
    SelectedPart: null,
    SingleOutputConnection: true,
    Vertical: true,
    lblDelete: 'Delete',
    lblNode: 'Node',
    Zoom:100,

    reset: function () {
        ffFlow.active = false;
        ffFlowPart.reset();
        this.FlowLines.reset();
        this.Mouse.reset();
    },

    eleFlowParts: null,
    zoom: function (percent) {
        if (ffFlow.eleFlowParts == null) {
            ffFlow.eleFlowParts = document.querySelector('.flow-parts');
        }
        ffFlow.Zoom = percent;
        ffFlow.eleFlowParts.style.zoom = percent / 100;
    },

    unSelect: function () {
        ffFlow.SelectedPart = null;
        ffFlowPart.unselectAll();
    },

    init: function (container, csharp, parts, elements) {
        ffFlow.csharp = csharp;
        ffFlow.parts = parts;
        ffFlow.elements = elements;
        ffFlow.infobox = null;


        ffFlow.csharp.invokeMethodAsync("Translate", `Labels.Delete`, null).then(result => {
            ffFlow.lblDelete = result;
        });

        ffFlow.csharp.invokeMethodAsync("Translate", `Labels.Node`, null).then(result => {
            ffFlow.lblNode = result;
        });

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
        container.addEventListener("touchstart", (e) => ffFlow.Mouse.dragStart(e), false);
        container.addEventListener("touchend", (e) => ffFlow.Mouse.dragEnd(e), false);
        container.addEventListener("touchmove", (e) => ffFlow.Mouse.drag(e), false);

        container.addEventListener("mousedown", (e) => ffFlow.Mouse.dragStart(e), false);
        container.addEventListener("mouseup", (e) => ffFlow.Mouse.dragEnd(e), false);
        container.addEventListener("mousemove", (e) => ffFlow.Mouse.drag(e), false);

        container.addEventListener("mouseup", (e) => ffFlow.FlowLines.ioMouseUp(e), false);
        container.addEventListener("mousemove", (e) => ffFlow.FlowLines.ioMouseMove(e), false);
        container.addEventListener("click", (e) => { ffFlow.unSelect() }, false);
        container.addEventListener("dragover", (e) => { ffFlow.drop(e, false) }, false);
        container.addEventListener("drop", (e) => { ffFlow.drop(e, true) }, false);



        let canvas = document.querySelector('canvas');

        let width = ffFlow.Vertical ? (document.body.clientWidth * 1.5) : window.screen.availWidth
        let height = ffFlow.Vertical ? (document.body.clientHeight * 2) : window.screen.availHeight;

        canvas.height = height;
        canvas.width = width;
        canvas.style.width = canvas.width + 'px';
        canvas.style.height = canvas.height + 'px';

        for (let p of parts) {
            try {
                ffFlowPart.addFlowPart(p);
            } catch (err) {
                if(p != null && p.name)
                    console.error(`Error adding flow part '${p.name}: ${err}`);
                else
                    console.error(`Error adding flow part: ${err}`);
            }
        }

        ffFlow.redrawLines();
    },

    redrawLines: function () {
        ffFlow.FlowLines.redrawLines();
    },


    ioInitConnections: function (connections) {
        ffFlow.reset();
        for (let k in connections) { // iterating keys so use in
            for (let con of connections[k]) { // iterating values so use of
                let id = k + '-output-' + con.output;

                let list = ffFlow.FlowLines.ioOutputConnections.get(id);
                if (!list) {
                    ffFlow.FlowLines.ioOutputConnections.set(id, []);
                    list = ffFlow.FlowLines.ioOutputConnections.get(id);
                }
                list.push({ index: con.input, part: con.inputNode });
            }

        }

        // this.ioOutputConnections.set(output, []);
        //     this.ioOutputConnections.get(output).push(input);
    },

    /*
     * Called from C# code to insert a new element to the flow
     */
    insertElement: function (uid) {
        ffFlow.drop(null, true, uid);
    },

    drop: function (event, dropping, uid) {
        let xPos = 100, yPos = 100;
        if (event) {
            event.preventDefault();
            if (dropping !== true)
                return;
            let bounds = event.target.getBoundingClientRect();

            xPos = ffFlow.translateCoord(event.clientX) - bounds.left - 20;
            yPos = ffFlow.translateCoord(event.clientY) - bounds.top - 20;
        } else {
        }
        if (!uid)
            uid = ffFlow.Mouse.draggingElementUid;


        ffFlow.csharp.invokeMethodAsync("AddElement", uid).then(result => {
            let element = result.element;
            if (!element) {
                console.warn('element was null');
                return;
            }
            let part = {
                name: '', // new part, dont set a name
                label: element.name,
                flowElementUid: element.uid,
                type: element.type,
                xPos: xPos - 30,
                yPos: yPos,
                inputs: element.model.Inputs ? element.model.Inputs : element.inputs,
                outputs: element.model.Outputs ? element.model.Outputs : element.outputs,
                uid: result.uid,
                icon: element.icon,
                model: element.model
            };

            if (part.model?.outputs)
                part.Outputs = part.model?.outputs;

            ffFlowPart.addFlowPart(part);
            ffFlow.parts.push(part);

            if (element.noEditorOnAdd === true)
                return;

            if (element.model && Object.keys(element.model).length > 0)
            {
                ffFlowPart.editFlowPart(part.uid, true);
            }
        });
    },

    translateCoord: function (value, lines) {
        if (lines !== true)
            value = Math.floor(value / 10) * 10;
        let zoom = ffFlow.Zoom / 100;
        if (!zoom || zoom === 1)
            return value;
        return value / zoom;
    },

    getModel: function () {
        console.log('getModel');
        let connections = this.FlowLines.ioOutputConnections;
        console.log('getting model, connections', connections);


        let connectionUids = [];
        for (let [outputPart, con] of connections) {
            connectionUids.push(outputPart);
            let partId = outputPart.substring(0, outputPart.indexOf('-output'));
            let output = parseInt(outputPart.substring(outputPart.lastIndexOf('-') + 1), 10);
            let part = this.parts.filter(x => x.uid === partId)[0];
            if (!part) {
                console.warn('unable to find part: ', partId);
                continue;
            }
            for (let inputCon of con) {
                let input = inputCon.index;
                let toPart = inputCon.part;
                if (!part.outputConnections)
                    part.outputConnections = [];

                if (ffFlow.SingleOutputConnection) {
                    console.log('removing output connections on output: ' + output);
                    // remove any duplciates from the output
                    part.outputConnections = part.outputConnections.filter(x => x.output != output);
                }

                part.outputConnections.push(
                    {
                        input: input,
                        output: output,
                        inputNode: toPart
                    });
            }
        }
        // remove any no longer existing connections
        for (let part of this.parts) {
            if (!part.outputConnections)
                continue;
            for (let i = part.outputConnections.length - 1; i >= 0;i--) {
                let po = part.outputConnections[i];
                let outUid = part.uid + '-output-' + po.output;
                if (connectionUids.indexOf(outUid) < 0) {
                    // need to remove it
                    part.outputConnections.splice(i, 1);
                }
            }
        }

        // update the part positions
        for (let p of this.parts) {
            var div = document.getElementById(p.uid);
            if (!div)
                continue;
            p.xPos = parseInt(div.style.left, 10);
            p.yPos = parseInt(div.style.top, 10);
        }

        console.log('model in js', this.parts);

        return this.parts;
    },

    getElement: function (uid) {
        console.log('getting element: ' + uid);
        return ffFlow.elements.filter(x => x.uid == uid)[0];
    },


    getPart: function (partUid) {
        return ffFlow.parts.filter(x => x.uid == partUid)[0];
    },

    infobox: null,
    infoboxSpan: null,
    infoSelectedType: '', 
    setInfo: function (message, type) {
        if (!message) {
            if (!ffFlow.infobox)
                return;
            ffFlow.infobox.style.display = 'none';
        } else {
            ffFlow.infoSelectedType = type;
            if (!ffFlow.infobox) {
                let box = document.createElement('div');
                box.classList.add('info-box');

                // remove button
                let remove = document.createElement('span');
                remove.classList.add('fas');
                remove.classList.add('fa-trash');
                remove.style.cursor = 'pointer';
                remove.setAttribute('title', ffFlow.lblDelete);
                remove.addEventListener("click", (e) => {
                    if (ffFlow.infoSelectedType === 'Connection')
                        ffFlow.FlowLines.deleteConnection();
                    else if (ffFlow.infoSelectedType === 'Node') {
                        if (ffFlow.SelectedPart)
                            ffFlowPart.deleteFlowPart(ffFlow.SelectedPart.uid);
                    }
                }, false);
                box.appendChild(remove);


                ffFlow.infoboxSpan = document.createElement('span');
                box.appendChild(ffFlow.infoboxSpan);


                document.getElementById('flow-parts').appendChild(box);
                ffFlow.infobox = box;
            }
            ffFlow.infobox.style.display = '';
            ffFlow.infoboxSpan.innerText = message;
        }
    },

    selectConnection: function (outputNode, output) {
        if (!outputNode) {
            ffFlow.setInfo();
            return;
        }

        let part = ffFlow.getPart(outputNode);
        if (!part) {
            ffFlow.setInfo();
            return;
        }

        if (!part.OutputLabels) {
            console.log('output labels null');
            return;
        }
        if (part.OutputLabels.length <= output) {
            console.log('output labels length less than output', output, part.OutputLabels);
            return;
        }
        ffFlow.setInfo(part.OutputLabels[output], 'Connection');
    },

    selectNode: function (part) {
        if (!part) {
            ffFlow.setInfo();
            return;
        }

        if (!part.displayDescription) {
            let element = ffFlow.getElement(part.flowElementUid);
            if (!element)
                return;
            ffFlow.csharp.invokeMethodAsync("Translate", `Flow.Parts.${element.name}.Description`, part.model).then(result => {
                part.displayDescription = ffFlow.lblNode + ': ' + (result === 'Description' || !result ? part.displayName : result);
                ffFlow.setInfo(part.displayDescription, 'Node');
            });
        } else {
            ffFlow.setInfo(part.displayDescription, 'Node');
        }
    },
    setOutputHint(part, output) {
        let element = ffFlow.getElement(part.flowElementUid);
        if (!element) {
            console.error("Failed to find element: " + part.flowElementUid);
            return;
        }
        console.log(element.name + '.model', part.model);
        ffFlow.csharp.invokeMethodAsync("Translate", `Flow.Parts.${element.name}.Outputs.${output}`, part.model).then(result => {
            if (!part.OutputLabels) part.OutputLabels = {};
            part.OutputLabels[output] = result;
            let outputNode = document.getElementById(part.uid + '-output-' + output);
            if (outputNode)
                outputNode.setAttribute('title', result);
        });
    },
    initOutputHints(part) {
        if (!part || !part.outputs)
            return;
        for (let i = 0; i < part.outputs; i++) {
            ffFlow.setOutputHint(part, i + 1);
        }
    }
}