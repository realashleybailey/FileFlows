class ffFlowHistory {
    history = [];
    redoActions = [];
    
    perform(action){
        // doing new action, anything past the current point we will clear
        this.redoActions = [];
        this.history.push(action);
        action.perform();        
    }
    
    redo() {
        if(this.redoActions.length === 0){1
            return; // nothing to redo
        }
        let action = this.redoActions.splice(0, 1)[0];
        this.history.push(action);
        action.redo();
    }
    
    undo(){
        if(this.history.length < 1) {
            return;
        }
        let action = this.history.pop();
        this.redoActions.push(action);
        action.undo();
    }    
}

class FlowActionMove {
    elementId;
    xPos;
    yPos;
    originalXPos;
    originalYPos;
    
    constructor(element, xPos, yPos, originalXPos, originalYPos) {
        // store the Id of the element, and not the actual element
        // incase the element is deleted then restored
        this.elementId = element.getAttribute('id');
        this.xPos = xPos;
        this.yPos = yPos;
        this.originalXPos = originalXPos;
        this.originalYPos = originalYPos;        
    }
    
    perform() {
        this.moveTo(this.xPos, this.yPos);
    }
    
    undo(){
        this.moveTo(this.originalXPos, this.originalYPos);
    }
    
    moveTo(x, y){
        let element = document.getElementById(this.elementId);
        if(!element)
            return;
        element.style.transform = '';
        element.style.left = x + 'px';
        element.style.top = y + 'px'

        ffFlow.redrawLines();
    }
}

class FlowActionDelete {

    html;
    parent;
    uid;
    ioOutputConnections;
    ffFlowPart;
    
    constructor(uid) {
        this.uid = uid;
        let element = document.getElementById(uid);
        this.parent = element.parentNode;
        this.html = element.outerHTML;
        this.ioOutputConnections = ffFlow.FlowLines.ioOutputConnections[this.uid];
        
        for (let i = 0; i < ffFlow.parts.length; i++) {
            if (ffFlow.parts[i].uid === this.uid) {
                this.ffFlowPart = ffFlow.parts[i];
            }
        }
    }

    perform() {
        var div = document.getElementById(this.uid);
        if (div) {
            ffFlowPart.flowPartElements = ffFlowPart.flowPartElements.filter(x => x != div);
            div.remove();
        }

        ffFlow.FlowLines.ioOutputConnections.delete(this.uid);

        for (let i = 0; i < ffFlow.parts.length; i++) {
            if (ffFlow.parts[i].uid === this.uid) {
                ffFlow.parts.splice(i, 1);
                break;
            }
        }
        

        ffFlow.setInfo();
        ffFlow.redrawLines();
    }

    undo(){        
        if(this.ffFlowPart)
            ffFlow.parts.push(this.ffFlowPart);
        
        // create the element again
        let div = document.createElement('div');
        div.innerHTML = this.html;
        let newPart = div.firstChild;
        newPart.classList.remove('selected');
        this.parent.appendChild(newPart);
        div.remove();
        ffFlowPart.flowPartElements.push(newPart);
        ffFlowPart.attachEventListeners({part: this.ffFlowPart, allEvents: true});

        // recreate the connections
        ffFlow.FlowLines.ioOutputConnections[this.uid] = this.ioOutputConnections;

        ffFlow.redrawLines();
    }
}

class FlowActionConnection {

    outputNodeUid;
    previousConnection;
    connection;

    constructor(outputNodeUid, connection) {
        this.outputNodeUid = outputNodeUid;
        this.connection = connection;
        this.previousConnection = ffFlow.FlowLines.ioOutputConnections.get(this.outputNodeUid);
    }

    perform() {
        this.connect(this.connection);
    }

    undo(){
        this.connect(this.previousConnection);
    }
    
    connect(connection){
        if(connection)
            ffFlow.FlowLines.ioOutputConnections.set(this.outputNodeUid, connection);
        else
            ffFlow.FlowLines.ioOutputConnections.delete(this.outputNodeUid);
        ffFlow.redrawLines();        
    }
}