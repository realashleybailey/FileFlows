window.ff = {
    log: function (level, parameters) {

        if (!parameters || parameters.length == 0)
            return;
        let message = parameters[0]
        parameters.splice(0, 1);

        if (level === 1) parameters.length > 0 ? console.error(message, parameters) : console.error(message);
        else if (level === 2) parameters.length > 0 ? console.warn(message, parameters) : console.warn(message);
        else if (level === 3) parameters.length > 0 ? console.info(message, parameters) : console.info(message);
        else parameters.length > 0 ? console.error(message, parameters) : console.log(message);
    },
    deviceDimensions: function () {

        return { width: screen.width, height: screen.height };
    },
    disableMovementKeys: function (element) {
        if (typeof (element) === 'string')
            element = document.getElementById(element);
        if (!element)
            return;
        const blocked = ['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Enter', 'PageUp', 'PageDown', 'Home', 'End'];
        element.addEventListener('keydown', e => {
            if (e.target.getAttribute('data-disable-movement') != 1)
                return;

            if (blocked.indexOf(e.code) >= 0) {
                e.preventDefault();
                return false;
            }
        })
    },
    loadJS: function (url, callback) {
        if (!url)
            return;
        let tag = document.createElement('script');
        tag.src = url;
        tag.onload = () => {
            if (callback)
                callback();
        };
        document.body.appendChild(tag);
    },
    downloadFile: function (url, filename) {
        const anchorElement = document.createElement('a');
        anchorElement.href = url;
        anchorElement.download = filename ? filename : 'File';
        anchorElement.click();
        anchorElement.remove();
    },
    copyToClipboard: function (text) {
        if (window.clipboardData && window.clipboardData.setData) {
            // Internet Explorer-specific code path to prevent textarea being shown while dialog is visible.
            return window.clipboardData.setData("Text", text);

        }
        else if (document.queryCommandSupported && document.queryCommandSupported("copy")) {
            var textarea = document.createElement("textarea");
            textarea.textContent = text;
            textarea.style.position = "fixed";  // Prevent scrolling to bottom of page in Microsoft Edge.
            document.body.appendChild(textarea);
            textarea.select();
            try {
                return document.execCommand("copy");  // Security exception may be thrown by some browsers.
            }
            catch (ex) {
                console.warn("Copy to clipboard failed.", ex);
                return prompt("Copy to clipboard: Ctrl+C, Enter", text);
            }
            finally {
                document.body.removeChild(textarea);
            }
        }
    }
};