public interface IWeldEffectController
{
    public void StartEffects(IWeldingTool tool, float power, float optimalPower);
    public void UpdateEffects(float power);
    public void StopEffects();
}