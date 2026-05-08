using UnityEngine;
using TMPro;

/// <summary>
/// Displays the player's gold count in the bottom-right of the screen.
/// 
/// SETUP:
/// 1. Create a UI element anchored to Bottom-Right.
/// 2. Add an Image child with CoinIcon_16x18.png.
/// 3. Add a TMP_Text child next to it for the gold amount.
/// 4. Attach this script and drag the text into the Inspector.
/// </summary>
public class GoldDisplay : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The TMP_Text component that shows the gold amount.")]
    public TMP_Text goldText;

    private int currentGold = 0;

    void Start()
    {
        UpdateDisplay();
    }

    /// <summary>
    /// Adds gold and refreshes the display.
    /// </summary>
    public void AddGold(int amount)
    {
        currentGold += amount;
        if (currentGold < 0) currentGold = 0;
        UpdateDisplay();
    }

    /// <summary>
    /// Sets the gold amount directly and refreshes the display.
    /// </summary>
    public void SetGold(int amount)
    {
        currentGold = Mathf.Max(0, amount);
        UpdateDisplay();
    }

    /// <summary>
    /// Returns the current gold count.
    /// </summary>
    public int GetGold()
    {
        return currentGold;
    }

    /// <summary>
    /// Attempts to spend gold. Returns true if successful, false if insufficient.
    /// </summary>
    public bool TrySpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            UpdateDisplay();
            return true;
        }
        return false;
    }

    private void UpdateDisplay()
    {
        if (goldText != null)
            goldText.text = currentGold.ToString();
    }
}
