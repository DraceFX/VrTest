using UnityEngine;

public class WeldHeat : MonoBehaviour
{
    public float coolSpeed = 1f;

    private MaterialPropertyBlock _mpb;
    private Renderer _rend;

    private float heat = 1f;

    private void Awake()
    {
        _rend = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }

    private void Update()
    {
        heat -= Time.deltaTime * coolSpeed;
        heat = Mathf.Clamp01(heat);

        _rend.GetPropertyBlock(_mpb);
        _mpb.SetFloat("_Heat", heat);
        _rend.SetPropertyBlock(_mpb);
    }
}