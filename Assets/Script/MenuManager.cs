using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public GameObject NotificationPanel;
    public GameObject CreditsPanel;

    public void StartGame()
    {
        SceneManager.LoadScene("Desktop");
    }

    public void ExitGame()
    {
        SceneManager.LoadScene("Title");
    }
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public void CloseNotification()
    {
        NotificationPanel.SetActive(false);
    }

    public void ToggleCredits()
    {
        if (CreditsPanel.activeInHierarchy)
        {
            CreditsPanel.SetActive(false);
        }
        else
        {
            CreditsPanel.SetActive(true);
        }
    }
}