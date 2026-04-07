using System.Collections;
using UnityEngine;

public class DetectionObject : MonoBehaviour, IDetection
{
    [SerializeField] private float _detectionTime = 5f;
    [SerializeField] private bool _isFake = false;

    [SerializeField] private AnswerButtonType _correctButton;

    public AnswerButtonType CorrectButton => _correctButton;

    private Coroutine _detectionCoroutine;

    private bool _isInZone = false;
    private bool _isDetected = false;

    private Renderer _rend;
    private Color _originColor;
    private Color _detectedColor = Color.green;

    private void Start()
    {
        _rend = GetComponent<Renderer>();
        _originColor = _rend.material.color;
    }


    public void EnterZone()
    {
        if (_isDetected) return;

        _isInZone = true;
        _detectionCoroutine = StartCoroutine(DetectionTimer());
    }

    public void ExitZone()
    {
        _isInZone = false;

        if (_detectionCoroutine != null)
        {
            StopCoroutine(_detectionCoroutine);
        }

        if (_isDetected)
        {
            ResetDetection();
        }
    }

    private IEnumerator DetectionTimer()
    {
        float time = 0f;

        while (time < _detectionTime)
        {
            if (!_isInZone) yield break;

            time += Time.deltaTime;
            yield return null;
        }

        CompleteDetection();
    }

    public void CompleteDetection()
    {
        _isDetected = true;
        _rend.material.color = _detectedColor;

        DetectionState.Set(this);
    }

    public void ResetDetection()
    {
        _isDetected = false;
        _rend.material.color = _originColor;

        DetectionState.Clear(this);
    }

    public bool IsDetected()
    {
        return _isDetected;
    }

    public bool IsFake()
    {
        return _isFake;
    }
}
