using UnityEngine;
using UnityEngine.SceneManagement;

public class UIControls : MonoBehaviour
{
    public void GoToLevel()
    {
        SceneManager.LoadScene("TestLevel");
    }

    public void QuitApp()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public bool ConfirmArea()
    {
        return true;
    }
}
