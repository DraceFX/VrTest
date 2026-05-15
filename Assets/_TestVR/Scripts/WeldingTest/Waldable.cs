using System.Collections.Generic;
using UnityEngine;

public class Weldable : MonoBehaviour
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private bool _isGrounded = false;

    [Header("Свойства сварки")]
    [SerializeField] private WeldProcessModel _processModel;

    public bool IsGrounded => _isGrounded;
    public WeldProcessModel ProcessModel => _processModel;

    private HashSet<Collider> _contactedColliders = new HashSet<Collider>();
    private bool _isClamped = false;

    public Rigidbody Rigidbody
    {
        get
        {
            if (_rb == null)
                _rb = GetComponentInParent<Rigidbody>();
            return _rb;
        }
    }

    private void Awake()
    {
        // Автоматически подхватываем Rigidbody, если он не назначен
        if (_rb == null)
            _rb = GetComponentInParent<Rigidbody>();
    }

    private void OnEnable() => GroundManager.RegisterWeldable(this);
    private void OnDisable() => GroundManager.UnregisterWeldable(this);

    private void OnCollisionEnter(Collision collision)
    {
        if (_contactedColliders.Add(collision.collider))
        {
            // Появился новый контакт — пересчитываем заземление
            GroundManager.NotifyGroundingChanged();
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (_contactedColliders.Remove(collision.collider))
        {
            // Контакт разорвался — снова пересчитываем
            GroundManager.NotifyGroundingChanged();
        }
    }

    internal void SetGroundedInternal(bool value)
    {
        _isGrounded = value;
    }

    public void SetClamped(bool value)
    {
        if (_isClamped == value) return;

        _isClamped = value;
        GroundManager.NotifyGroundingChanged();
    }

    public void RefreshGrounding()
    {
        if (_isClamped)
        {
            _isGrounded = true;
            return;
        }

        foreach (var col in _contactedColliders)
        {
            if (col == null) continue;

            GroundSurface gs = col.GetComponentInParent<GroundSurface>();
            if (gs != null && gs.IsActive)
            {
                _isGrounded = true;
                return;
            }

            Weldable w = col.GetComponentInParent<Weldable>();
            if (w != null && w._isGrounded)
            {
                _isGrounded = true;
                return;
            }
        }

        _isGrounded = false;
    }
}