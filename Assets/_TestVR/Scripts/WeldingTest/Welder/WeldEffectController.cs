using UnityEngine;

public class WeldEffectController : MonoBehaviour
{
    private Electrode _currentElectrode;
    private bool _effectsPlaying;

    public void StartEffects(Electrode electrode, float power, float optimalPower)
    {
        if (electrode == null) return;
        if (_effectsPlaying && _currentElectrode == electrode) return;

        StopEffects();
        _currentElectrode = electrode;
        _currentElectrode.StartWeldEffects(power, optimalPower);
        _effectsPlaying = true;
    }

    public void UpdateEffects(float power)
    {
        if (_effectsPlaying && _currentElectrode != null)
            _currentElectrode.UpdateWeldEffects(power);
    }

    public void StopEffects()
    {
        if (_effectsPlaying && _currentElectrode != null)
        {
            _currentElectrode.StopWeldEffects();
            _effectsPlaying = false;
            _currentElectrode = null;
        }
    }
}