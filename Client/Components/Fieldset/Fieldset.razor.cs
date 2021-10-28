namespace FileFlow.Client.Components
{
    using Microsoft.AspNetCore.Components;
    using FileFlow.Shared;

    public partial class Fieldset : ComponentBase
    {
        private string _Title;
        private string _OriginalTitle;
        [Parameter]
        public string Title
        {
            get => _Title;
            set
            {
                if (_OriginalTitle == value)
                    return;
                _OriginalTitle = value;
                _Title = Translater.TranslateIfNeeded(value);
            }
        }

        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}