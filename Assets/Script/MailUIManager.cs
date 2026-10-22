using UnityEngine;

public class MailUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject SearchBar;
    public GameObject Mail;
    public GameObject MailDetails;

    public void ToggleMail()
    {
        Debug.Log("Mail UI Toggled");
        if (MailDetails.activeInHierarchy)
        {
            Mail.SetActive(true);
            MailDetails.SetActive(false);
            SearchBar.SetActive(true);
        }
        else
        {
            Mail.SetActive(false);
            MailDetails.SetActive(true);
            SearchBar.SetActive(false);
        }
    }
}
