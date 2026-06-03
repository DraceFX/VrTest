using System;

public interface IToolSocket
{
    // Текущий прикреплённый инструмент (может быть Electrode, MIGTorch, …)
    public IWeldingTool AttachedTool { get; }

    // События прикрепления/открепления
    public event Action<IWeldingTool> ToolAttached;
    public event Action ToolDetached;

    // Базовые операции
    public void Attach(IWeldingTool tool);
    public void Detach(IWeldingTool tool);
}
