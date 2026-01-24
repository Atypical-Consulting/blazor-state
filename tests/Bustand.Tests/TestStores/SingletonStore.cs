using Bustand.Attributes;
using Bustand.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Bustand.Tests.TestStores;

public record SingletonState(string Config = "default");

[BustandStore(ServiceLifetime.Singleton)]
public class SingletonStore : ZustandStore<SingletonState>
{
    protected override SingletonState InitialState => new SingletonState();
}
