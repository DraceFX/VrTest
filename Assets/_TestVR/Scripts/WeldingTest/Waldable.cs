using System.Collections.Generic;
using UnityEngine;

public class Weldable : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private bool isGrounded = false;

    public bool IsGrounded => isGrounded;

    private HashSet<Collider> contactedColliders = new HashSet<Collider>();
    private bool isClamped = false;

    public Rigidbody Rigidbody
    {
        get
        {
            if (rb == null)
                rb = GetComponentInParent<Rigidbody>();
            return rb;
        }
    }

    private void Awake()
    {
        // Автоматически подхватываем Rigidbody, если он не назначен
        if (rb == null)
            rb = GetComponentInParent<Rigidbody>();
    }

    private void OnEnable() => GroundManager.RegisterWeldable(this);
    private void OnDisable() => GroundManager.UnregisterWeldable(this);

    private void OnCollisionEnter(Collision collision)
    {
        if (contactedColliders.Add(collision.collider))
        {
            // Появился новый контакт — пересчитываем заземление
            GroundManager.NotifyGroundingChanged();
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (contactedColliders.Remove(collision.collider))
        {
            // Контакт разорвался — снова пересчитываем
            GroundManager.NotifyGroundingChanged();
        }
    }

    internal void SetGroundedInternal(bool value)
    {
        isGrounded = value;
    }

    public void SetClamped(bool value)
    {
        if (isClamped == value) return;
        isClamped = value;
        GroundManager.NotifyGroundingChanged();
    }

    public void RefreshGrounding()
    {
        if (isClamped)
        {
            isGrounded = true;
            return;
        }

        foreach (var col in contactedColliders)
        {
            if (col == null) continue;

            GroundSurface gs = col.GetComponentInParent<GroundSurface>();
            if (gs != null && gs.IsActive)
            {
                isGrounded = true;
                return;
            }

            Weldable w = col.GetComponentInParent<Weldable>();
            if (w != null && w.isGrounded)
            {
                isGrounded = true;
                return;
            }
        }

        isGrounded = false;
    }
}