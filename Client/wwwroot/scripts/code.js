window.ffCode = {

    initModel: function (variables) {

        monaco.editor.createModel(
            "const Logger = { ILog: function(...any), DLog: function(...any), WLog: function(...any), ELog: function(...any) }",
            "javascript"
        );

        monaco.editor.createModel(
            "const Flow = { " +
            "CreateDirectoryIfNotExists: function (path:string)," +
            "GetDirectorySize: function (path: string):number, " +
            "GetParameter: function (key: string):any, " +
            "MapPath: function (path: string):string, " +
            "MoveFile: function (destination: string), " +
            "ResetWorkingFile: function (), " +
            "SetWorkingFile: function (filename: string, dontDelete: bool), " +
            "SetParameter: function (key:string, value:any) " +
            "}",
            "javascript"
        );


        if (variables) {
            var actualVaraibles = {};
            for (let k in variables) {
                let tk = k;
                let av = actualVaraibles
                while (tk.indexOf('.') > 0) {
                    let nk = tk.substring(0, tk.indexOf('.'));
                    if(!av[nk])
                        av[nk] = {};
                    tk = tk.substring(tk.indexOf('.') + 1);
                    av = av[nk];
                }
                if(!av[tk])
                    av[tk] = variables[k]
            }
            let js = "const Variables = " + JSON.stringify(actualVaraibles);
            monaco.editor.createModel(js, "javascript");

        }
    }
}