using ArcGIS.Desktop.Framework.Contracts;

namespace Hearth.ArcGIS.Samples.PlugIns.Contracts
{
    public class InjectableButton : Button, IInjectable
    {
        public InjectableButton()
        {
            this.InjectServices();
        }
    }
}
