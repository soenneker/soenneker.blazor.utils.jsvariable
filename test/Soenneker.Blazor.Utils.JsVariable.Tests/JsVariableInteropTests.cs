using Soenneker.Blazor.Utils.JsVariable.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Blazor.Utils.JsVariable.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class JsVariableInteropTests : HostedUnitTest
{
    private readonly IJsVariableInterop _util;

    public JsVariableInteropTests(Host host) : base(host)
    {
        _util = Resolve<IJsVariableInterop>(true);
    }

    [Test]
    public void Default()
    {

    }
}
