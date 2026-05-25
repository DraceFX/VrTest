using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WeldAssembly : MonoBehaviour
{
    [Header("Настройки прочности шва")]
    [SerializeField] private float _fullQualityBreakForce = 10000f;   // при 100% качестве
    [SerializeField] private float _fullQualityBreakTorque = 10000f; // при 100% качестве


    private FixedJoint _joint;
    private Rigidbody _connectedBody; // для отслеживания
    private List<GameObject> _weldMeshObjects = new List<GameObject>();  // все меши между этой парой
    private float _totalQuality = 0f;
    private bool _isDestroyed = false;

    public bool IsBroken => _joint == null;
    public float CurrentBreakForce => _joint != null ? _joint.breakForce : 0f;
    public float CurrentBreakTorque => _joint != null ? _joint.breakTorque : 0f;

    public static WeldAssembly Create(Weldable a, Weldable b, float quality, GameObject weldMeshObject = null)
    {
        if (a == null || b == null)
        {
            Debug.LogError("[WeldAssembly] Один из Weldable равен null!");
            return null;
        }

        Rigidbody rbA = a.GetComponentInParent<Rigidbody>();
        Rigidbody rbB = b.GetComponentInParent<Rigidbody>();

        if (rbA == null || rbB == null)
        {
            Debug.LogError("[WeldAssembly] Оба объекта должны иметь Rigidbody!");
            return null;
        }

        WeldAssembly existing = null;
        foreach (var assemble in rbA.GetComponents<WeldAssembly>())
        {
            if (assemble._connectedBody == rbB)
            {
                existing = assemble;
                break;
            }
        }

        if (existing != null)
        {
            // Добавляем качество к существующему соединению
            float newTotalQuality = Mathf.Min(1f, existing._totalQuality + quality);
            existing._totalQuality = newTotalQuality;

            // Обновляем прочность джойнта
            existing._joint.breakForce = newTotalQuality * existing._fullQualityBreakForce;
            existing._joint.breakTorque = newTotalQuality * existing._fullQualityBreakTorque;

            // Добавляем новый меш в список
            if (weldMeshObject != null)
                existing._weldMeshObjects.Add(weldMeshObject);

            Debug.Log($"[WeldAssembly] Существующее соединение обновлено: {a.name} ↔ {b.name}, суммарное качество: {newTotalQuality:P0}, breakForce={existing._joint.breakForce}");
            return existing;
        }

        // Создаём новое соединение
        FixedJoint joint = rbA.gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = rbB;
        joint.enableCollision = false;
        joint.breakForce = quality * 10000f;   // начальная прочность
        joint.breakTorque = quality * 10000f;

        WeldAssembly assembly = rbA.gameObject.AddComponent<WeldAssembly>();
        assembly._joint = joint;
        assembly._connectedBody = rbB;
        assembly._totalQuality = quality;
        if (weldMeshObject != null)
            assembly._weldMeshObjects.Add(weldMeshObject);

        Debug.Log($"[WeldAssembly] Создано новое соединение: {a.name} ↔ {b.name}, качество: {quality:P0}, breakForce={joint.breakForce}");
        return assembly;
    }

    private void OnJointBreak(float breakForce)
    {
        if (_isDestroyed) return;
        _isDestroyed = true;

        Debug.Log($"[WeldAssembly] Сварной шов разрушен! Приложенная сила: {breakForce}");

        // Удаляем все связанные меши
        foreach (var mesh in _weldMeshObjects)
        {
            if (mesh != null) Destroy(mesh);
        }
        _weldMeshObjects.Clear();

        // Убираем джойнт и компонент
        if (_joint != null) Destroy(_joint);
        Destroy(this);
    }
}