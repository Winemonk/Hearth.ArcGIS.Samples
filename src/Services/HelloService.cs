using ArcGIS.Desktop.Framework.Dialogs;

namespace Hearth.ArcGIS.Samples.Services
{
    [Service(typeof(IHelloService))]
    public class HelloService : IHelloService
    {
        public void SayHello()
        {
            MessageBox.Show("Hello, World!", this.GetType().Name);
        }
    }
}
