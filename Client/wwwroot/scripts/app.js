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
    gotoHomePage: function () {
        window.open('https://github.com/revenz/fileflows', '_blank');
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
    }
};