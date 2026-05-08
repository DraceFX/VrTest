using UnityEngine;

public class Weldable : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;

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
}