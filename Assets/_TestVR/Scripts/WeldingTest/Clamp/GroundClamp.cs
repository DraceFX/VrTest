using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GroundClamp : MonoBehaviour
{
    [Header("Точка контакта")]
    [SerializeField] private Transform _contactPoint;
    [SerializeField] private float _contactRadius = 0.05f;

    [Header("Состояние")]
    [SerializeField] private bool _isAttached = false;

    [Header("Анимация")]
    [SerializeField] private Transform _jawTransform;        // подвижная часть (губка)
    [SerializeField] private Vector3 _openRotation = new Vector3(0, 0, 30f); // на сколько повернуть в открытом состоянии
    [SerializeField] private float _animationSpeed = 8f;     // скорость поворота

    private Rigidbody _rb;
    private FixedJoint _joint;
    private Weldable _clampedWeldable;   // запоминаем, кого заземлили
    private XRGrabInteractable _xrGrab;

    private bool _isOpen = false;
    private bool _triggerHeld = false;
    private Quaternion _closedRotation;
    private Quaternion _targetJawRotation;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
            Debug.LogError("GroundClamp требует Rigidbody");

        _xrGrab = GetComponent<XRGrabInteractable>();

        _xrGrab.activated.AddListener(OnActivated);
        _xrGrab.deactivated.AddListener(OnDeactivated);

        if (_jawTransform != null)
        {
            _closedRotation = _jawTransform.localRotation;
            _targetJawRotation = _closedRotation;
        }
    }

    private void Update()
    {
        // Плавный поворот челюсти к цели
        if (_jawTransform != null)
        {
            _jawTransform.localRotation = Quaternion.RotateTowards(_jawTransform.localRotation, _targetJawRotation, _animationSpeed * Time.deltaTime);
        }
    }

    private void OnDestroy()
    {
        if (_xrGrab != null)
        {
            _xrGrab.activated.RemoveListener(OnActivated);
            _xrGrab.deactivated.RemoveListener(OnDeactivated);
        }
    }

    private void OnJointBreak(float breakForce)
    {
        Detach();
        SetOpen(false);
    }

    private void OnActivated(ActivateEventArgs args)
    {
        _triggerHeld = true;

        if (_isAttached)
        {
            // Отсоединяем при нажатии триггера на прикреплённой клемме
            Detach();
            _isAttached = false;
        }

        // В любом случае открываем клемму
        SetOpen(true);
    }

    private void OnDeactivated(DeactivateEventArgs args)
    {
        _triggerHeld = false;

        if (_isOpen && !_isAttached)
        {
            // Пытаемся захватить, если рядом есть подходящий объект
            if (TryFindTarget(out Rigidbody targetRb))
            {
                AttachTo(targetRb);
            }
        }

        // Закрываем клемму
        SetOpen(false);
    }

    private bool TryFindTarget(out Rigidbody targetRb)
    {
        targetRb = null;
        Collider[] hits = Physics.OverlapSphere(_contactPoint.position, _contactRadius);
        foreach (var hit in hits)
        {
            Rigidbody hitRb = hit.GetComponentInParent<Rigidbody>();
            if (hitRb != null)
            {
                targetRb = hitRb;
                return true;
            }
        }
        return false;
    }

    private void AttachTo(Rigidbody targetRb)
    {
        if (_isAttached) return;

        _joint = gameObject.AddComponent<FixedJoint>();
        _joint.connectedBody = targetRb;
        _joint.autoConfigureConnectedAnchor = true;
        _joint.breakForce = 5000f;
        _joint.breakTorque = 1000f;
        _joint.enableCollision = false;

        _isAttached = true;

        _clampedWeldable = targetRb.GetComponentInParent<Weldable>();
        if (_clampedWeldable != null)
        {
            _clampedWeldable.SetClamped(true);
        }
    }

    public void Detach()
    {
        if (!_isAttached) return;

        if (_clampedWeldable != null)
        {
            _clampedWeldable.SetClamped(false);
            _clampedWeldable = null;
        }

        if (_joint != null)
        {
            Destroy(_joint);
            _joint = null;
        }

        _isAttached = false;
    }

    private void SetOpen(bool open)
    {
        _isOpen = open;

        if (_jawTransform != null)
        {
            _targetJawRotation = open ? _closedRotation * Quaternion.Euler(_openRotation) : _closedRotation;
        }
        else
        {
            Debug.LogWarning("GroundClamp: _jawTransform не назначен. Анимация не работает.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_contactPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_contactPoint.position, _contactRadius);
        }
    }
}