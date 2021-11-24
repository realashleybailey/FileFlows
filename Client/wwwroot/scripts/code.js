window.ffCode = {

    initModel: function (variables) {

        monaco.editor.createModel(
            "const Logger = { ILog: function(...any), DLog: function(...any), WLog: function(...any), ELog: function(...any) }",
            "javascript"
        );

        if (variables) {
            let js = "const Variables = " + JSON.stringify(variables);
            monaco.editor.createModel(js, "javascript");

        }
    }
}