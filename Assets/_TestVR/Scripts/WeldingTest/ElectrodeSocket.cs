using UnityEngine;

public class ElectrodeSocket : MonoBehaviour, IToolSocket
{
    [Header("Настройки крепления")]
    [SerializeField] private Transform _attachPoint;      // Точка, к которой прикрепляется электрод
    [SerializeField] private Vector3 _rotationAxis = Vector3.right; // Ось вращения (X, Y, Z)
    [SerializeField] private float[] _fixedAngles = new float[] { 0f, 45f, 90f, 135f };

    public Electrode AttachedElectrode { get; private set; }

    public IWeldingTool AttachedTool => AttachedElectrode as IWeldingTool;
    public event System.Action<IWeldingTool> ToolAttached;
    public event System.Action ToolDetached;

    // Пытается прикрепить электрод к держателю.
    public void TryAttachElectrode(Electrode electrode)
    {
        if (AttachedElectrode != null)
            return;

        Quaternion localRotation = Quaternion.Inverse(_attachPoint.rotation) * electrode.transform.rotation;
        float currentAngle = GetAngleAroundAxis(localRotation, _rotationAxis);
        currentAngle = (currentAngle % 360f + 360f) % 360f;

        float closestAngle = FindClosestAngle(currentAngle);

        AttachedElectrode = electrode;
        electrode.AttachedSocket = this;
        electrode.CurrentSocket = null;

        electrode.transform.SetParent(_attachPoint);
        electrode.transform.localPosition = Vector3.zero;
        electrode.transform.localRotation = Quaternion.AngleAxis(closestAngle, _rotationAxis);

        electrode.Rb.isKinematic = true;

        // Вызываем старые и новые события
        ToolAttached?.Invoke(electrode as IWeldingTool);
    }

    /// Открепляет электрод от держателя.
    public void DetachElectrode(Electrode electrode)
    {
        if (AttachedElectrode != electrode)
            return;

        AttachedElectrode = null;
        electrode.AttachedSocket = null;
        electrode.transform.SetParent(null);

        if (electrode.Rb != null)
            electrode.Rb.isKinematic = false;

        ToolDetached?.Invoke();
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

    public void Attach(IWeldingTool tool)
    {
        if (tool is Electrode electrode)
            DetachElectrode(electrode);
    }

    public void Detach(IWeldingTool tool)
    {
        if (tool is Electrode electrode)
            DetachElectrode(electrode);
    }
}