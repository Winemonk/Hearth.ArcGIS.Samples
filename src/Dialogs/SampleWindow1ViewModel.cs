using ArcGIS.Desktop.Framework.Contracts;

namespace Hearth.ArcGIS.Samples.Dialogs
{
    public class SampleWindow1ViewModel : ViewModelBase
    {
        private string _sampleText = "Sample Text";
        public string SampleText
        {
            get => _sampleText;
            set => SetProperty(ref _sampleText, value, () => SampleText);
        }
    }
}
