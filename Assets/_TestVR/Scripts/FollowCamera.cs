using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Vector3 _offfset;

    private void Update()
    {
        transform.position = _cameraTransform.position + _offfset;
        transform.rotation = _cameraTransform.rotation;
    }
}
