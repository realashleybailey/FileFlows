window.ffCode = {

    initModel: function () {
        
        monaco.editor.createModel(
            "const VideoFile = { Codec: '', File: '', AudioTracks:[], Size: 0, Duration: ''}",
            "javascript"
        );
        
        monaco.editor.createModel(
            "const Logger = { ILog: function(...any), DLog: function(...any), WLog: function(...any), ELog: function(...any) }",
            "javascript"
        );
    }
}