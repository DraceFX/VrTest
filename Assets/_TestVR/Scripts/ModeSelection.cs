using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeSelection : MonoBehaviour
{
    [SerializeField] private string _mainSceneName = "GameMechanic";

    public void SelectVR()
    {
        ModeManager.IsPCMode = false;
        SceneManager.LoadScene(_mainSceneName);
    }

    public void SelectPC()
    {
        ModeManager.IsPCMode = true;
        SceneManager.LoadScene(_mainSceneName);
    }
}
