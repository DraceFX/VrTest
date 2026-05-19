using UnityEngine;

public class WeldElectrodeConsumer : MonoBehaviour
{
    public void ConsumeElectrode(Electrode electrode, WeldProcessModel model, float power)
    {
        if (model == null || electrode == null) return;

        float melt = model.EvaluateMelt(power) * Time.deltaTime;
        electrode.Burn(melt);
    }
}