using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeldZone : MonoBehaviour
{
    public Rigidbody bodyA;
    public Rigidbody bodyB;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out WeldTorch torch))
            torch.EnterZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out WeldTorch torch))
            torch.ExitZone(this);
    }
}