using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Soenneker.Blazor.Utils.JsVariable.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;


namespace Soenneker.Blazor.Utils.JsVariable.Tests;

[Collection("Collection")]
public class JsVariableInteropTests : FixturedUnitTest
{
    private readonly IJsVariableInterop _interop;

    public JsVariableInteropTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _interop = Resolve<IJsVariableInterop>(true);
    }
}
