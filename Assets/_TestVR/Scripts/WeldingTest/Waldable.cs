using UnityEngine;

public class Weldable : MonoBehaviour
{
    public Rigidbody rb;
    public float health = 100f;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    public void ApplyDamage(float dmg)
    {
        health -= dmg;

        if (health <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
