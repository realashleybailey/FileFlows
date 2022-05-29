namespace FileFlows.Plugin
{

    public enum FlowElementType
    {
        Input,
        Output,
        Process,
        Logic,
        BuildStart,
        BuildEnd,
        BuildPart,
        Failure,
        Communication
    }

    public enum FormInputType
    {
        Text = 1,
        Switch = 2,
        Select = 3,
        TextArea = 4,
        Code = 5,
        Int = 6,
        Float = 7,
        StringArray = 8,
        File = 9,
        Folder = 10,
        LogView = 11,
        RegularExpression = 12,
        TextVariable = 13,
        KeyValue = 14,
        Label = 15,
        HorizontalRule = 16,
        Schedule = 17,
        Slider = 18,
        Checklist = 19,
        TextLabel = 20,
        Password = 21,
        ExecutedNodes = 22,
        Table = 23
    }


}