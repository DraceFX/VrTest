using UnityEngine;

public class ElectrodeSocket : MonoBehaviour
{
    [Header("Настройки крепления")]
    [SerializeField] private Transform _attachPoint;      // Точка, к которой прикрепляется электрод
    [SerializeField] private Vector3 _rotationAxis = Vector3.right; // Ось вращения (X, Y, Z)
    [SerializeField] private float[] _fixedAngles = new float[] { 0f, 45f, 90f, 135f };

    public Electrode AttachedElectrode { get; private set; }

    public System.Action<Electrode> OnElectrodeAttached;
    public System.Action OnElectrodeDetached;


    // Пытается прикрепить электрод к держателю.
    public void TryAttachElectrode(Electrode electrode)
    {
        if (AttachedElectrode != null)
            return;

        // 1. Вычисляем текущий угол электрода относительно оси вращения
        Quaternion localRotation = Quaternion.Inverse(_attachPoint.rotation) * electrode.transform.rotation;
        float currentAngle = GetAngleAroundAxis(localRotation, _rotationAxis);
        currentAngle = (currentAngle % 360f + 360f) % 360f; // Нормализуем в [0, 360)

        // 2. Находим ближайший фиксированный угол
        float closestAngle = FindClosestAngle(currentAngle);

        // 3. Фиксируем электрод
        AttachedElectrode = electrode;
        electrode.AttachedSocket = this;
        electrode.CurrentSocket = null;

        electrode.transform.SetParent(_attachPoint);
        electrode.transform.localPosition = Vector3.zero;
        electrode.transform.localRotation = Quaternion.AngleAxis(closestAngle, _rotationAxis);

        electrode.Rb.isKinematic = true; // Отключаем физику

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
        electrode.Rb.isKinematic = false;

        OnElectrodeDetached?.Invoke();
    }

    private float FindClosestAngle(float currentAngle)
    {
        float closest = _fixedAngles[0];
        float minDiff = Mathf.Abs(Mathf.DeltaAngle(currentAngle, _fixedAngles[0]));

        for (int i = 1; i < _fixedAngles.Length; i++)
        {
            float diff = Mathf.Abs(Mathf.DeltaAngle(currentAngle, _fixedAngles[i]));
            if (diff < minDiff)
            {
                minDiff = diff;
                closest = _fixedAngles[i];
            }
        }
        return closest;
    }

    //Извлекает угол поворота вокруг заданной оси из кватерниона
    private float GetAngleAroundAxis(Quaternion rotation, Vector3 axis)
    {
        Vector3 euler = rotation.eulerAngles;

        if (Mathf.Abs(axis.x) > Mathf.Abs(axis.y) && Mathf.Abs(axis.x) > Mathf.Abs(axis.z))
            return euler.x * Mathf.Sign(axis.x);
        else if (Mathf.Abs(axis.y) > Mathf.Abs(axis.z))
            return euler.y * Mathf.Sign(axis.y);
        else
            return euler.z * Mathf.Sign(axis.z);
    }
}