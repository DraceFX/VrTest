using UnityEngine;

public class EnableRenderCamera : MonoBehaviour
{
    [SerializeField] private Camera _cameraRender;
    [SerializeField] private Canvas _canvasRender;

    private void Awake()
    {
        _cameraRender.gameObject.SetActive(false);
        _canvasRender.gameObject.SetActive(false);
    }

    public void HoverTablet(bool isHover)
    {
        _cameraRender.gameObject.SetActive(isHover);
        _canvasRender.gameObject.SetActive(isHover);
    }

}
