using TMPro;
using UnityEngine;

public class VrPerformanceMonitor : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text _text;

    [Header("Update Settings")]
    [SerializeField] private float _updateInterval = 0.5f;

    private float _timer;
    private float _frameTime;
    private float _fps;
    private int _frameCount;

    private void Update()
    {
        _frameCount++;
        _timer += Time.unscaledDeltaTime;

        if (_timer >= _updateInterval)
        {
            _fps = _frameCount / _timer;
            _frameTime = (_timer / _frameCount) * 1000f;

            UpdateUI();

            _frameCount = 0;
            _timer = 0f;
        }
    }

    private void UpdateUI()
    {
        float memory = (float)System.GC.GetTotalMemory(false) / (1024f * 1024f);

        _text.text = $"FPS: {_fps:F1}\n" + $"Frame: {_frameTime:F2} ms\n" + $"Memory: {memory:F1} MB";
    }
}
