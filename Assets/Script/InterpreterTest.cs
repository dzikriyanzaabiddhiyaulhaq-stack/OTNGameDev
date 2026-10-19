using TMPro;
using UnityEngine;

public class InterpreterTest : MonoBehaviour
{
    [SerializeField] private TMP_InputField CodeInput;
    [SerializeField] private TMP_Text OutputText;

    private MiniPythonInterpreter interpreter = new MiniPythonInterpreter();

    public void RunCode()
    {
        Debug.Log("RunCode() was called!");

        Debug.Log("Input: " + CodeInput.text);

        string result = interpreter.Run(CodeInput.text);

        Debug.Log("Interpreter result: [" + result + "]");

        OutputText.text = string.IsNullOrEmpty(result)
        ? "(No output produced)"
        : result;
    }
}
