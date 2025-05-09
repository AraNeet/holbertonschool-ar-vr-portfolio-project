using UnityEngine;

public class UIManager
{
    private GameObject placementIndicator;
    private GameObject instructionPanel;
    
    public UIManager(GameObject placementIndicator, GameObject instructionPanel)
    {
        this.placementIndicator = placementIndicator;
        this.instructionPanel = instructionPanel;
    }
    
    public void ShowPlacementUI()
    {
        if (placementIndicator != null)
            placementIndicator.SetActive(true);
            
        if (instructionPanel != null)
            instructionPanel.SetActive(true);
    }
    
    public void HidePlacementUI()
    {
        if (placementIndicator != null)
            placementIndicator.SetActive(false);
            
        if (instructionPanel != null)
            instructionPanel.SetActive(false);
    }
    
    public void SetPlacementIndicatorActive(bool active)
    {
        if (placementIndicator != null)
            placementIndicator.SetActive(active);
    }
    
    public void SetInstructionPanelActive(bool active)
    {
        if (instructionPanel != null)
            instructionPanel.SetActive(active);
    }
} 