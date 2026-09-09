using TMPro;
using UnityEngine;

public class BirthdayPanelController : MonoBehaviour
{
    [Header("UI")]
    public GameObject panelRoot;          // drag BirthdayPanel here
    public TMP_InputField birthdayInput;  // drag BirthdayInput here

    [Header("Calendar Spin Script")]
    public InnerRingMover spinner;        // drag the object with InnerRingMover here

    void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void ShowPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (birthdayInput != null)
        {
            birthdayInput.text = "";
            birthdayInput.Select();
            birthdayInput.ActivateInputField();
        }
    }

    public void SubmitBirthday()
    {
        // Don't worry about date parsing yet — just trigger spin.
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (spinner != null)
            spinner.StartSpin();
    }

    public void HidePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}