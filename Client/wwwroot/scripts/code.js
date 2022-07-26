window.ffCode = {
    
    initModel: function (variables, sharedScripts) {
        
        // console.log('sharedScripts', sharedScripts);
        // if(sharedScripts?.length){
        //     for(let script of sharedScripts){
        //         monaco.editor.languages.typescript.javascriptDefaults.addExtraLib(`declare module '../Shared/${script.name}' {{ export interface ${script.name} {{}} }}`, `/script/code/${script.Name}`);
        //     }
        // }
        
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
            "SetParameter: function (key:string, value:any)," +
            "NewGuid: function ():string," +
            "Execute: function(args:{command:string, arguments: string, argumentList:string[], timeout:number, workingDirectory:string}):{completed:bool, exitCode:number, output:string, standardOutput:string, starndardError:string}," +
            "TempPath:string," +
            "WorkingFile:string," +
            "WorkingFileSize:number," +
            "RelativeFile:string," +
            "IsDirectory:bool" +
            "LibraryPath:string," +
            "}",
            "javascript"
        );
        
        const funFileInfo = `declare function FileInfo(path: string): {
   Exists: bool,
   Length: number,
   Name: string, 
   DirectoryName: string, 
   IsReadOnly: bool,
   CreationTime: date,
   LastWriteTime: date, 
   LastAccessTime: date,
   Extension: string
};`;
        monaco.languages.typescript.javascriptDefaults.addExtraLib(funFileInfo, 'ff.funFileInfo');
        
        monaco.editor.createModel(
            "const Sleep = function(milliseconds)",
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