using ArcGIS.Desktop.Framework.Dialogs;

namespace Hearth.ArcGIS.Samples.Services
{
    [Service(typeof(IHearthHelloService))]
    public class HearthHelloService : IHearthHelloService
    {
        private readonly IHelloService _helloService;
        public HearthHelloService(IHelloService helloService)
        {
            _helloService = helloService;
        }

        public void SayHello()
        {
            _helloService.SayHello();
            MessageBox.Show("Hello, Hearth!", this.GetType().Name);
        }
    }
}
