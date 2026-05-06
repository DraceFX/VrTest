using UnityEngine;

public class ElectrodeSocket : MonoBehaviour
{
    [Header("Точка крепления электрода")]
    public Transform electrodeTransform;

    [Header("Локальная ось вращения электрода")]
    public Vector3 rotationAxis = Vector3.right; // Обычно X, чтобы наклонять "вперёд-назад"

    [Header("Фиксированные углы (в градусах)")]
    public float[] fixedAngles = new float[] { 0f, 45f, 90f, 135f };

    public Electrode AttachedElectrode { get; private set; }

    public System.Action<Electrode> OnElectrodeAttached;
    public System.Action OnElectrodeDetached;

    /// Пытается прикрепить электрод к держателю.
    public void TryAttachElectrode(Electrode electrode)
    {
        if (AttachedElectrode != null)
            return; // Уже занято

        // Вычисляем текущий локальный поворот электрода относительно electrodeTransform
        Quaternion localRotation = Quaternion.Inverse(electrodeTransform.rotation) * electrode.transform.rotation;
        float currentAngle = GetAngleAroundAxis(localRotation, rotationAxis);

        // Нормализуем в диапазон [0, 360)
        currentAngle = (currentAngle % 360f + 360f) % 360f;

        // Ищем ближайший угол из массива
        float closestAngle = fixedAngles[0];
        float minDiff = Mathf.Abs(Mathf.DeltaAngle(currentAngle, fixedAngles[0]));
        for (int i = 1; i < fixedAngles.Length; i++)
        {
            float diff = Mathf.Abs(Mathf.DeltaAngle(currentAngle, fixedAngles[i]));
            if (diff < minDiff)
            {
                minDiff = diff;
                closestAngle = fixedAngles[i];
            }
        }

        // Фиксируем электрод
        AttachedElectrode = electrode;
        electrode.AttachedSocket = this;
        electrode.CurrentSocket = null;

        // Настраиваем Transform
        electrode.transform.SetParent(electrodeTransform);
        electrode.transform.localPosition = Vector3.zero;       // Электрод должен быть смоделирован так, чтобы его основание совпадало с этой точкой
        electrode.transform.localRotation = Quaternion.AngleAxis(closestAngle, rotationAxis);

        // Отключаем физику
        electrode.rb.isKinematic = true;

        AttachedElectrode = electrode;
        OnElectrodeAttached?.Invoke(electrode);
    }

    /// Открепляет электрод от держателя.
    public void DetachElectrode(Electrode electrode)
    {
        if (AttachedElectrode != electrode)
            return;

        AttachedElectrode = null;

        electrode.AttachedSocket = null;
        electrode.transform.SetParent(null);
        electrode.rb.isKinematic = false;

        AttachedElectrode = null;
        OnElectrodeDetached?.Invoke();
    }

    /// <summary>Извлекает угол поворота вокруг заданной оси из кватерниона.</summary>
    private float GetAngleAroundAxis(Quaternion rotation, Vector3 axis)
    {
        // Для простоты работаем через углы Эйлера, если ось — одна из стандартных
        Vector3 euler = rotation.eulerAngles;

        if (Mathf.Abs(axis.x) > Mathf.Abs(axis.y) && Mathf.Abs(axis.x) > Mathf.Abs(axis.z)) return euler.x * Mathf.Sign(axis.x);
        else if (Mathf.Abs(axis.y) > Mathf.Abs(axis.z)) return euler.y * Mathf.Sign(axis.y);
        else return euler.z * Mathf.Sign(axis.z);
    }
}