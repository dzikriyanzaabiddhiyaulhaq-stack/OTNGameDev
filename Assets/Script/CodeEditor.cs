using System;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using Unity.AI.Navigation.LowLevel;
using UnityEngine;

public class CodeEditor : MonoBehaviour
{
    [Header("Editor UI")]
    [SerializeField] private TMP_InputField FileNameInput;
    [SerializeField] private TMP_InputField CodeInput;
    [SerializeField] private TMP_Text OverlayText;
    [SerializeField] private TMP_Text StatusText;

    private string desktopFolder;
    private string currentFilePath;

    void Awake()
    {
        CodeInput.textComponent.color = new Color(1, 1, 1, 1);
        CodeInput.textComponent.faceColor = new Color(0, 0, 0, 0);

        CodeInput.onValueChanged.AddListener(OnTextChanged);

        string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(documentsFolder))
        {
            documentsFolder = Application.persistentDataPath;
        }

        desktopFolder = Path.Combine(
            documentsFolder,
            "Ondel",
            "Desktop"
        );

        Directory.CreateDirectory(desktopFolder);

        NewFile();
    }

    private void OnTextChanged(string rawText)
    {
        string highlightedText = SyntaxHighlighter.Highlight(rawText);
        OverlayText.text = highlightedText + "\u200B";
    }

    private string GetSafeFilePath()
    {
        string input = FileNameInput.text.Trim();

        input = Path.GetFileName(input);
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Please enter a filename.");
        }

        string NameWithoutExtension = Path.GetFileNameWithoutExtension(input);

        if (string.IsNullOrWhiteSpace(NameWithoutExtension) || NameWithoutExtension.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("That filename is not valid!");
        }

        return Path.Combine(desktopFolder, NameWithoutExtension + ".py");
    }

    public void SaveFile()
    {
        try
        {
            string path = GetSafeFilePath();
            File.WriteAllText(path, CodeInput.text);
            currentFilePath = path;

            FileNameInput.text = Path.GetFileName(path);
            SetStatus("Saved" + Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            SetStatus("Save Failed: " + ex.Message);
        }
    }

    public void LoadFile()
    {
        try
        {
            string path = GetSafeFilePath();
            if (!File.Exists(path))
            {
                SetStatus("File not found.");
                return;
            }

            CodeInput.text = File.ReadAllText(path);
            currentFilePath = path;
            FileNameInput.text = Path.GetFileName(path);
            SetStatus("Loaded " + Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            SetStatus("Load Failed; " + ex.Message);
        }
    }

    public void NewFile()
    {
        currentFilePath = null;
        FileNameInput.text = "untitled.py";
        CodeInput.text = "";
    }

    private void SetStatus(string message)
    {
        if (StatusText != null)
        {
            StatusText.text = message;
        }
    }
}
