using System;

public interface IStepCompletionProvider
{
    public event Action OnCompleted;
}
