using UnityEngine;

public class FlowCanvas : MonoBehaviour
{
    [SerializeField] bool disableOnStart;

    [Header("Target")]
    [SerializeField] private Transform headCamera;

    [Header("Position")]
    [SerializeField] private float distanceFromCamera = 1.2f;
    [SerializeField] private Vector3 localOffset = Vector3.zero;

    [Header("Rotation")]
    [SerializeField] private bool followYawOnly = true;

    [Header("Follow Delay")]
    [Range(0f, 10f)]
    [SerializeField] private float followDelay = 2f;

    [SerializeField] private float maxSmoothTime = 1.5f;

    private Vector3 _currentVelocity;

    private void Start()
    {
        if (headCamera == null && Camera.main != null)
            headCamera = Camera.main.transform;

        if (headCamera == null)
        {
            Debug.LogError("VRMenuFollowHead: не назначена камера.");
            enabled = false;
            return;
        }

        if (disableOnStart)
        {
            gameObject.SetActive(false);
        }

        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (headCamera == null)
            return;

        Vector3 targetPosition = GetTargetPosition();
        Quaternion targetRotation = GetTargetRotation();

        if (followDelay <= 0f)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            return;
        }

        float smoothTime = Mathf.Lerp(0.01f, maxSmoothTime, followDelay / 10f);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref _currentVelocity,
            smoothTime);

        float rotationLerpSpeed = Mathf.Lerp(25f, 2f, followDelay / 10f);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationLerpSpeed * Time.deltaTime);
    }

    private Vector3 GetTargetPosition()
    {
        Vector3 forward;

        if (followYawOnly)
        {
            forward = headCamera.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
                forward = headCamera.parent != null ? headCamera.parent.forward : Vector3.forward;

            forward.Normalize();
        }
        else
        {
            forward = headCamera.forward.normalized;
        }

        Vector3 basePosition = headCamera.position + forward * distanceFromCamera;

        Vector3 offsetWorld =
            headCamera.right * localOffset.x +
            headCamera.up * localOffset.y +
            headCamera.forward * localOffset.z;

        return basePosition + offsetWorld;
    }

    private Quaternion GetTargetRotation()
    {
        if (followYawOnly)
        {
            Vector3 direction = transform.position - headCamera.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                direction = headCamera.forward;

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
        else
        {
            Vector3 direction = transform.position - headCamera.position;

            if (direction.sqrMagnitude < 0.0001f)
                direction = headCamera.forward;

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    [ContextMenu("Snap To Target")]
    public void SnapToTarget()
    {
        if (headCamera == null)
            return;

        transform.position = GetTargetPosition();
        transform.rotation = GetTargetRotation();
        _currentVelocity = Vector3.zero;
    }

    public void SetFollowDelay(float value)
    {
        followDelay = Mathf.Clamp(value, 0f, 10f);
    }
}
