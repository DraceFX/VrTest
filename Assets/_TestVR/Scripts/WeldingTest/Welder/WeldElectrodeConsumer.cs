using UnityEngine;

public class WeldElectrodeConsumer : MonoBehaviour, IWeldConsumableConsumer
{
    public void Consume(IWeldingTool tool, WeldProcessModel model, float power)
    {
        if (model == null || tool == null || !tool.IsConsumable)
            return;

        float melt = model.EvaluateMelt(power) * Time.deltaTime;
        tool.Consume(melt);
    }
}