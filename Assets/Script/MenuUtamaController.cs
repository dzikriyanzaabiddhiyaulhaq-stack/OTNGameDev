using UnityEngine;
using UnityEngine.SceneManagement;

// Pasang di GameObject kosong "MenuUtamaManager" di scene "menu utama"
public class MenuUtamaController : MonoBehaviour
{
    [Header("Nama scene")]
    [SerializeField] private string menuHomeScene = "menu home";
    [SerializeField] private string mapLevelScene = "Map Level"; // ganti sesuai scene map kota dari temen

    [Header("Panel 'aplikasi' (jendela ala desktop)")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject itemBoostPanel;

    private void Start()
    {
        CloseAllPanels();
    }

    // Ikon aplikasi "Upgrade Karyawan"
    public void OnOpenUpgrade()
    {
        CloseAllPanels();
        upgradePanel.SetActive(true);
    }

    // Ikon aplikasi "Item Boost"
    public void OnOpenItemBoost()
    {
        CloseAllPanels();
        itemBoostPanel.SetActive(true);
    }

    // Tombol X di tiap jendela
    public void CloseAllPanels()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (itemBoostPanel != null) itemBoostPanel.SetActive(false);
    }

    // Ikon / tombol "Mulai Main"
    public void OnStartPlay()
    {
        SceneManager.LoadScene(mapLevelScene);
    }

    // Opsional: tombol kembali ke menu home
    public void OnBackToHome()
    {
        SceneManager.LoadScene(menuHomeScene);
    }
}
