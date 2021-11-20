class ffFlowMouse {
    dragItem = null;
    currentX = 0;
    currentY = 0;
    initialX = 0;
    initialY = 0;
    xOffset = 0;
    yOffset = 0;
    draggingElementUid = null;

    reset() {
        this.currentX = 0;
        this.currentY = 0;
        this.initialX = 0;
        this.initialY = 0;
        this.xOffset = 0;
        this.yOffset = 0;
        this.dragItem = null;
        this.draggingElementUid = null;
    }

    dragElementStart(event) {
        this.draggingElementUid = event.target.id;
        event.dataTransfer.setData("text", event.target.id);
        event.dataTransfer.effectAllowed = "copy";
    }

    dragStart(e) {
        if (e.type === "touchstart") {
            this.initialX = e.touches[0].clientX - this.xOffset;
            this.initialY = e.touches[0].clientY - this.yOffset;
        } else {
            this.initialX = e.clientX;// - this.xOffset;
            this.initialY = e.clientY;// - this.yOffset;
        }


        if (e.target.classList.contains('draggable') === true) {
            this.currentX = 0;
            this.currentY = 0;
            this.dragItem = e.target.parentNode;
            ffFlow.active = true;

        }
    }

    dragEnd(e) {
        this.initialX = this.currentX;
        this.initialY = this.currentY;
        if (ffFlow.active && this.dragItem) {
            this.dragItem.style.transform = '';
            let xPos = parseInt(this.dragItem.style.left, 10) + this.currentX;
            let yPos = parseInt(this.dragItem.style.top, 10) + this.currentY;
            this.dragItem.style.left = xPos + 'px';
            this.dragItem.style.top = yPos + 'px';


            ffFlow.redrawLines();
        }
        ffFlow.active = false;
    }

    drag(e) {
        if (ffFlow.active) {

            e.preventDefault();

            if (e.type === "touchmove") {
                this.currentX = e.touches[0].clientX - this.initialX;
                this.currentY = e.touches[0].clientY - this.initialY;
            } else {
                this.currentX = e.clientX - this.initialX;
                this.currentY = e.clientY - this.initialY;
            }


            this.xOffset = this.currentX;
            this.yOffset = this.currentY;

            this.setTranslate(this.currentX, this.currentY, this.dragItem);
            ffFlow.redrawLines();
        }
    }

    setTranslate(xPos, yPos, el) {
        el.style.transform = "translate3d(" + xPos + "px, " + yPos + "px, 0)";
    }
}