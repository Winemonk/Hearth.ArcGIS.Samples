using Hearth.ArcGIS.Samples.PlugIns.Contracts;
using Hearth.ArcGIS.Samples.Services;

namespace Hearth.ArcGIS.Samples.PlugIns.Menus
{
    internal class SampleButton1 : InjectableButton
    {
        [Inject]
        private readonly IHearthHelloService? _helloService;

        protected override void OnClick()
        {
            _helloService?.SayHello();
        }
    }
}
