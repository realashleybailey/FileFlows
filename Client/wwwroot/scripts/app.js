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
    }
};