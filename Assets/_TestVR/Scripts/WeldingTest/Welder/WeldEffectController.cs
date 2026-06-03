using UnityEngine;

public class WeldEffectController : MonoBehaviour, IWeldEffectController
{
    private IWeldingTool _currentTool;
    private bool _effectsPlaying;

    public void StartEffects(IWeldingTool tool, float power, float optimalPower)
    {
        if (tool == null) return;
        if (_effectsPlaying && _currentTool == tool) return;

        StopEffects();
        _currentTool = tool;
        _currentTool.StartWeldEffects(power, optimalPower);
        _effectsPlaying = true;
    }

    public void UpdateEffects(float power)
    {
        if (_effectsPlaying && _currentTool != null)
            _currentTool.UpdateWeldEffects(power);
    }

    public void StopEffects()
    {
        if (_effectsPlaying && _currentTool != null)
        {
            _currentTool.StopWeldEffects();
            _effectsPlaying = false;
            _currentTool = null;
        }
    }
}