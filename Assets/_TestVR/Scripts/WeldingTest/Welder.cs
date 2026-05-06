using UnityEngine;

public class Welder : MonoBehaviour
{
    [Header("Сокет держателя")]
    public ElectrodeSocket socket;

    [Header("Настройки сварки")]
    public WeldingSettings settings;
    public WeldProcessModel process = new WeldProcessModel();

    [Header("Шов")]
    public Material weldMaterial;
    public GameObject beadPrefab;

    private bool isActivated;
    private BeadWeldGenerator currentWeld;

    public void SetActivated(bool state)
    {
        isActivated = state;

        if (!state)
            currentWeld = null;
    }

    private void Update()
    {
        if (!isActivated)
            return;

        if (settings == null)
            return;

        Electrode electrode = socket.AttachedElectrode;
        if (electrode == null)
            return;

        float power = settings.Power;

        HandleElectrode(electrode, power);
        TryWeld(electrode, power);
    }

    private void HandleElectrode(Electrode electrode, float power)
    {
        float melt = process.EvaluateMelt(power) * Time.deltaTime;

        electrode.Burn(melt);
    }

    private void TryWeld(Electrode electrode, float power)
    {
        Ray ray = new Ray(electrode.transform.position, electrode.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hitA, electrode.weldDistance))
            return;

        Weldable a = hitA.collider.GetComponent<Weldable>();
        if (a == null)
            return;

        float deposit = process.EvaluateDeposit(power);

        UpdateMesh(a.rb, hitA.point, hitA.normal);

        currentWeld.spacing = Mathf.Lerp(
            0.006f,
            0.002f,
            deposit * 100f
        );

        // ИЩЕМ ВТОРОЙ ОБЪЕКТ ДЛЯ СВАРКИ
        Collider[] hits = Physics.OverlapSphere(hitA.point, 0.05f);

        Weldable b = null;

        foreach (var col in hits)
        {
            if (col.transform == hitA.transform)
                continue;

            b = col.GetComponent<Weldable>();
            if (b != null)
                break;
        }

        // СОЕДИНЯЕМ ТОЛЬКО ДВА WELDABLE
        if (b != null)
        {
            CreateJointIfNeeded(a.rb, b.rb);
        }

        if (process.IsBurning(power))
        {
            HandleBurnThrough(hitA);
        }
    }

    private void HandleBurnThrough(RaycastHit hit)
    {
        Debug.Log("BURN THROUGH!");

        // вариант 1: урон
        var weldable = hit.collider.GetComponent<Weldable>();
        if (weldable != null)
        {
            weldable.ApplyDamage(10f * Time.deltaTime);
        }

        // вариант 2: визуальный эффект
        Debug.DrawRay(hit.point, hit.normal * 0.05f, Color.red, 0.1f);
    }

    private void CreateJointIfNeeded(Rigidbody a, Rigidbody b)
    {
        if (a == null || b == null)
            return;

        // проверка, чтобы не дублировать соединение
        FixedJoint[] joints = a.GetComponents<FixedJoint>();
        foreach (var j in joints)
        {
            if (j.connectedBody == b)
                return;
        }

        // создаём двустороннюю связь (симметрия шва)
        FixedJoint jointA = a.gameObject.AddComponent<FixedJoint>();
        jointA.connectedBody = b;
        jointA.breakForce = Mathf.Infinity;
        jointA.breakTorque = Mathf.Infinity;

        FixedJoint jointB = b.gameObject.AddComponent<FixedJoint>();
        jointB.connectedBody = a;
        jointB.breakForce = Mathf.Infinity;
        jointB.breakTorque = Mathf.Infinity;
    }

    private void UpdateMesh(Rigidbody parent, Vector3 point, Vector3 normal)
    {
        if (currentWeld == null)
        {
            GameObject go = new GameObject("WeldBeads");
            go.transform.SetParent(parent.transform);

            currentWeld = go.AddComponent<BeadWeldGenerator>();
            currentWeld.beadPrefab = beadPrefab;
        }

        currentWeld.AddPoint(point, normal);
    }
}