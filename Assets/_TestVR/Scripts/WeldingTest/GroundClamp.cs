using UnityEngine;

public class GroundClamp : MonoBehaviour
{
    [Header("Точка контакта")]
    public Transform contactPoint;
    public float contactRadius = 0.05f;

    [Header("Состояние")]
    [SerializeField] private bool isAttached = false;

    private Rigidbody rb;
    private FixedJoint joint;
    private Weldable clampedWeldable;   // запоминаем, кого заземлили

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.LogError("GroundClamp требует Rigidbody");
    }

    public void TryAttach()
    {
        if (isAttached) return;

        Collider[] hits = Physics.OverlapSphere(contactPoint.position, contactRadius);
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
        joint = gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = targetRb;
        joint.autoConfigureConnectedAnchor = true;

        rb.isKinematic = true;
        isAttached = true;

        // Пытаемся найти Weldable на цели и "заземлить" его
        clampedWeldable = targetRb.GetComponentInParent<Weldable>();
        if (clampedWeldable != null)
        {
            clampedWeldable.SetClamped(true);
        }
    }

    public void Detach()
    {
        if (!isAttached) return;

        // Сначала снимаем заземление с Weldable, если был
        if (clampedWeldable != null)
        {
            clampedWeldable.SetClamped(false);
            clampedWeldable = null;
        }

        // Убираем физическое соединение
        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }

        rb.isKinematic = false;
        isAttached = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (contactPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(contactPoint.position, contactRadius);
        }
    }
}