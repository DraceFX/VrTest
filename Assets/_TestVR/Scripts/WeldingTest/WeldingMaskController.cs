using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WeldingMaskController : MonoBehaviour
{
    [Header("Углы маски (локальные, по оси X)")]
    public float upAngle = 0f;      // Поднятое состояние
    public float downAngle = -85f;  // Опущенное состояние (сварка)
    public float snapThreshold = -45f; // Порог срабатывания авто-фиксации
    public float smoothSpeed = 8f;  // Скорость плавного возврата

    private XRGrabInteractable grab;
    private Quaternion upRot, downRot, targetRot;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        upRot = Quaternion.Euler(upAngle, 0, 0);
        downRot = Quaternion.Euler(downAngle, 0, 0);
        targetRot = upRot;
        transform.localRotation = upRot;

        // Подписываемся на события захвата
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs e)
    {
        // При захвате отключаем автоматический возврат
    }

    private void OnRelease(SelectExitEventArgs e)
    {
        // Определяем, в какую сторону "отпустить" маску
        float x = transform.localEulerAngles.x;
        if (x > 180f) x -= 360f; // Нормализуем в диапазон -180..180

        targetRot = x < snapThreshold ? downRot : upRot;
    }

    void LateUpdate()
    {
        // Плавный возврат к целевому углу
        if (!grab.isSelected)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * smoothSpeed);
        }

        // Жёсткое ограничение вращения только по оси X
        float x = transform.localEulerAngles.x;
        if (x > 180f) x -= 360f;
        x = Mathf.Clamp(x, downAngle, upAngle);
        transform.localRotation = Quaternion.Euler(x, 0f, 0f);
    }
}
