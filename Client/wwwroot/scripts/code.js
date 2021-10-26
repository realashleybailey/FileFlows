window.ViCode = {

    initModel: function () {
        
        const defModel = monaco.editor.createModel(
            "const VideoFile = { Codec: '', File: '', AudioTracks:[], Size: 0, Duration: ''}",
            "javascript"
        );
    }
}