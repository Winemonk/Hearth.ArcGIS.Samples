using ArcGIS.Desktop.Framework.Contracts;
using DryIoc;
using Hearth.ArcGIS.Samples.Services;

namespace Hearth.ArcGIS.Samples.PlugIns.Menus
{
    internal class SampleButton2 : Button, IInjectable
    {
        protected override void OnClick()
        {
            SampleTest sampleTest = new SampleTest();
            sampleTest.Test();
        }
    }

    public class SampleTest: IInjectable
    {
        private readonly IHearthHelloService? _privateReadOnly;
        private IHearthHelloService? _private;
        public IHearthHelloService? _public;

        public SampleTest()
        {
            this.InjectPropertiesAndFields();
        }

        public void Test()
        {
            _privateReadOnly?.SayHello(); // NULL
            _private?.SayHello();
            _public?.SayHello();
        }
    }
}
