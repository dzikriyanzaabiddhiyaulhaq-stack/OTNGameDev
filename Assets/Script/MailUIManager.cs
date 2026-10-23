using TMPro;
using UnityEngine;

public class MailUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject MailApp;
    public GameObject SearchBar;
    public GameObject Mail;
    public GameObject MailDetails;

    public void Update()
    {
        if (!MailApp.activeInHierarchy)
        {
            Mail.SetActive(true);
            MailDetails.SetActive(false);
            SearchBar.SetActive(true);
        }
    }

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
