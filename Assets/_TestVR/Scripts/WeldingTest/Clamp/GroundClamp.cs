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

    private Rigidbody _rb;
    private FixedJoint _joint;
    private Weldable _clampedWeldable;   // запоминаем, кого заземлили
    private XRGrabInteractable _xrGrab;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
            Debug.LogError("GroundClamp требует Rigidbody");

        _xrGrab = GetComponent<XRGrabInteractable>();

        _xrGrab.activated.AddListener(OnActivated);
    }

    private void OnDestroy()
    {
        if (_xrGrab != null)
        {
            _xrGrab.activated.RemoveListener(OnActivated);
        }
    }

    private void OnActivated(ActivateEventArgs args)
    {
        if (_isAttached)
            Detach();
        else
            TryAttach();
    }

    public void TryAttach()
    {
        if (_isAttached) return;

        Collider[] hits = Physics.OverlapSphere(_contactPoint.position, _contactRadius);
        foreach (var hit in hits)
        {
            Rigidbody hitRb = hit.GetComponentInParent<Rigidbody>();
            if (hitRb == null) continue;

            // Крепимся к первому попавшемуся Rigidbody
            AttachTo(hitRb);
            return;
        }
    }

    private void AttachTo(Rigidbody targetRb)
    {
        // Физическое соединение
        _joint = gameObject.AddComponent<FixedJoint>();
        _joint.connectedBody = targetRb;
        _joint.autoConfigureConnectedAnchor = true;

        // _rb.isKinematic = true;
        _isAttached = true;

        // Пытаемся найти Weldable на цели и "заземлить" его
        _clampedWeldable = targetRb.GetComponentInParent<Weldable>();
        if (_clampedWeldable != null)
        {
            _clampedWeldable.SetClamped(true);
        }
    }

    public void Detach()
    {
        if (!_isAttached) return;

        // Сначала снимаем заземление с Weldable, если был
        if (_clampedWeldable != null)
        {
            _clampedWeldable.SetClamped(false);
            _clampedWeldable = null;
        }

        // Убираем физическое соединение
        if (_joint != null)
        {
            Destroy(_joint);
            _joint = null;
        }

        // _rb.isKinematic = false;
        _isAttached = false;
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