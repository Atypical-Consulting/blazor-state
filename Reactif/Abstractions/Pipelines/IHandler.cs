namespace Reactif.Abstractions.Pipelines;

public interface IHandler<in TInput, out TOutput>
    : IObserver<TInput>, IObservable<TOutput>
{
}