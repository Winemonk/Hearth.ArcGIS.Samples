using ArcGIS.Desktop.Framework.Dialogs;

namespace Hearth.ArcGIS.Samples.Services
{
    public class HelloService : IHelloService
    {
        public void SayHello()
        {
            MessageBox.Show("Hello, World!", this.GetType().Name);
        }
    }
}
