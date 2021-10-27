window.Vi = {
    log: function (level, message, parameters) {
        if (level === 1) parameters ? console.error(message, parameters) : console.error(message);
        else if (level === 2) parameters ? console.warn(message, parameters) : console.warn(message);
        else if (level === 3) parameters ? console.info(message, parameters) : console.info(message);
        else  parameters ? console.error(message, parameters) : console.log(message);
    }
}