using UnityEngine;

public interface IWeldDefectEngine
{
    public void ProcessDefects(WeldMeshBuilder builder, WeldProcessModel model, float power, float finalQuality,
                                                Vector3 point, Vector3 normal, IWeldingTool tool, RaycastHit hit);
}
