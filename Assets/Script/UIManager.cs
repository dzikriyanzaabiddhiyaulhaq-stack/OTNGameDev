using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Aplikasi")]
    public GameObject Editor;
    public GameObject Hax;
    public GameObject Folder;
    public GameObject Mail;
    public GameObject Quest;

    // Buka
    public void OpenEditor()
    {
        Debug.Log("Klik masuk!");
        Editor.SetActive(true);
    }
    public void OpenHax()    { Hax.SetActive(true); }
    public void OpenFolder() { Folder.SetActive(true); }
    public void OpenMail()   { Mail.SetActive(true); }
    public void OpenQuest()  { Quest.SetActive(true); }

    // Tutup
    public void CloseEditor() { Editor.SetActive(false); }
    public void CloseHax()    { Hax.SetActive(false); }
    public void CloseFolder() { Folder.SetActive(false); }
    public void CloseMail()   { Mail.SetActive(false); }
    public void CloseQuest()  { Quest.SetActive(false); }
}