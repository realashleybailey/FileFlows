namespace ViWatcher.Shared
{
    public enum NodeResult {
        Failure = 0,
        Success = 1,
    }

    public enum FlowElementType {
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
        StringArray = 8
    }


}