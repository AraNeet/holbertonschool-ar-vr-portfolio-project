using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelector : MonoBehaviour
{
    [SerializeField] private Button mageButton;
    [SerializeField] private Button barbButton;
    [SerializeField] private TextMeshProUGUI selectionText;

    private bool isMageSelected = true;

    void Start()
    {
        UpdateSelectionUI();

        // Add listeners to buttons
        if (mageButton != null)
        {
            mageButton.onClick.AddListener(() => SelectCharacter(true));
        }

        if (barbButton != null)
        {
            barbButton.onClick.AddListener(() => SelectCharacter(false));
        }
    }

    public void SelectCharacter(bool isMage)
    {
        isMageSelected = isMage;
        UpdateSelectionUI();
    }

    private void UpdateSelectionUI()
    {
        if (mageButton != null)
        {
            mageButton.interactable = !isMageSelected;
        }

        if (barbButton != null)
        {
            barbButton.interactable = isMageSelected;
        }

        if (selectionText != null)
        {
            selectionText.text = $"Selected: {(isMageSelected ? "Mage" : "Barbarian")}";
        }
    }

    public void ConfirmSelection()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCharacter(isMageSelected);
            gameObject.SetActive(false);
        }
    }
}