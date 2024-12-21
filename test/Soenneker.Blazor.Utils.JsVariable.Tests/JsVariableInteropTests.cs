using Soenneker.Blazor.Utils.JsVariable.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;


namespace Soenneker.Blazor.Utils.JsVariable.Tests;

[Collection("Collection")]
public class JsVariableInteropTests : FixturedUnitTest
{
    private readonly IJsVariableInterop _util;

    public JsVariableInteropTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IJsVariableInterop>(true);
    }

    [Fact]
    public void Default()
    {

    }
}
