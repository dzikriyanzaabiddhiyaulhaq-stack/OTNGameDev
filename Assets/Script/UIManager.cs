using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Aplikasi")]
    public GameObject Editor;
    public GameObject Folder;
    public GameObject Mail;
    public GameObject Quest;
    public GameObject ExitButton;

    // Buka
    public void OpenEditor()
    {
        Debug.Log("Klik masuk!");
        Editor.SetActive(true);
    }
    
    public void OpenFolder() { Folder.SetActive(true); }
    public void OpenMail()   { Mail.SetActive(true); }
    public void OpenQuest()  { Quest.SetActive(true); }

    // Tutup
    public void CloseEditor() { Editor.SetActive(false); }
    public void CloseFolder() { Folder.SetActive(false); }
    public void CloseMail()   { Mail.SetActive(false); }
    public void CloseQuest()  { Quest.SetActive(false); }
    public void ExitGame() {SceneManager.LoadScene("Title");}
}