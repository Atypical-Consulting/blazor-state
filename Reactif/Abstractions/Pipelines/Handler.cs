using System.Reactive.Subjects;

namespace Reactif.Abstractions.Pipelines;

public abstract class Handler<TInput, TOutput>
    : IHandler<TInput, TOutput>
{
    private readonly Subject<TOutput> _subject = new();

    public virtual void OnNext(TInput value)
    {
        var transformed = Handle(value);
        _subject.OnNext(transformed);
    }

    public virtual void OnError(Exception error)
    {
        _subject.OnError(error);
    }

    public virtual void OnCompleted()
    {
        _subject.OnCompleted();
    }

    protected abstract TOutput Handle(TInput input);

    public IDisposable Subscribe(IObserver<TOutput> observer)
    {
        return _subject.Subscribe(observer);
    }
}