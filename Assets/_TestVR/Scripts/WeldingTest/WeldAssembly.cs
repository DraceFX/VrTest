using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WeldAssembly : MonoBehaviour
{
    public static WeldAssembly Create(Weldable a, Weldable b, Vector3 weldPoint)
    {
        if (a == null || b == null)
        {
            Debug.LogError("[WeldAssembly] Один из Weldable равен null!");
            return null;
        }

        // Получаем Rigidbody родителей
        Rigidbody rbA = a.GetComponentInParent<Rigidbody>();
        Rigidbody rbB = b.GetComponentInParent<Rigidbody>();

        // --- 1. СНАЧАЛА удаляем зависимые XRGrabInteractable с самих объектов A и B ---
        XRGrabInteractable grabA = a.GetComponent<XRGrabInteractable>();
        XRGrabInteractable grabB = b.GetComponent<XRGrabInteractable>();
        if (grabA != null) Destroy(grabA);
        if (grabB != null) Destroy(grabB);

        // --- 2. Теперь замораживаем физику ---
        if (rbA != null) rbA.isKinematic = true;
        if (rbB != null) rbB.isKinematic = true;

        // --- 3. Создаём общий родительский объект ---
        GameObject assemblyGO = new GameObject("WeldedAssembly");
        assemblyGO.transform.position = (a.transform.position + b.transform.position) * 0.5f;
        assemblyGO.transform.rotation = Quaternion.identity;

        Rigidbody assemblyRb = assemblyGO.AddComponent<Rigidbody>();
        float massA = rbA ? rbA.mass : 1f;
        float massB = rbB ? rbB.mass : 1f;
        assemblyRb.mass = massA + massB;
        assemblyRb.useGravity = true;
        assemblyRb.isKinematic = false;
        assemblyRb.linearVelocity = Vector3.zero;
        assemblyRb.angularVelocity = Vector3.zero;

        // --- 4. Переносим A и B как дети ---
        a.transform.SetParent(assemblyGO.transform, true);
        b.transform.SetParent(assemblyGO.transform, true);

        // --- 5. Удаляем старые Rigidbody (Grab'ов уже нет) ---
        if (rbA != null) Destroy(rbA);
        if (rbB != null) Destroy(rbB);

        // --- 6. Подчищаем оставшиеся Grab'ы у детей (на всякий случай) ---
        foreach (var grab in assemblyGO.GetComponentsInChildren<XRGrabInteractable>())
        {
            if (grab.gameObject != assemblyGO)
                Destroy(grab);
        }

        // --- 7. Добавляем общий XRGrabInteractable на сборку ---
        var assemblyGrab = assemblyGO.AddComponent<XRGrabInteractable>();
        assemblyGrab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        assemblyGrab.throwOnDetach = false;

        assemblyGO.layer = a.gameObject.layer;
        var weldAssembly = assemblyGO.AddComponent<WeldAssembly>();

        Debug.Log($"[WeldAssembly] Создана сборка из {a.name} и {b.name}");
        return weldAssembly;
    }
}