using Hearth.ArcGIS.Samples.Dialogs;
using Hearth.ArcGIS.Samples.PlugIns.Contracts;

namespace Hearth.ArcGIS.Samples.PlugIns.Menus
{
    internal class OpenSampleWindow1Button : InjectableButton
    {
        protected override void OnClick()
        {
            SampleWindow1 window = new SampleWindow1();
            window.ShowDialog();
        }
    }
}
