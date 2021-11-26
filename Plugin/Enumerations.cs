namespace FileFlows.Plugin
{

    public enum FlowElementType
    {
        Input,
        Output,
        Process,
        Logic
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
        KeyValue = 14
    }


}