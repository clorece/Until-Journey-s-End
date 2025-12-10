using UnityEngine;
using System;

public class KeybindManager : MonoBehaviour
{
    [Header("Basic Actions (Type Key Names)")]
    public string basicAttackKey = "Mouse0"; 
    public string interactKey = "F";
    public string jumpKey = "Space";

    [Header("Skill Hotkeys")]

    public string[] skillKeyStrings; 

    [HideInInspector] public KeyCode basicAttack;
    [HideInInspector] public KeyCode interact;
    [HideInInspector] public KeyCode jump;
    [HideInInspector] public KeyCode[] skillKeys; 

    void Awake()
    {
        basicAttack = ParseKey(basicAttackKey);
        interact = ParseKey(interactKey);
        jump = ParseKey(jumpKey);

        skillKeys = new KeyCode[skillKeyStrings.Length];
        for (int i = 0; i < skillKeyStrings.Length; i++)
        {
            skillKeys[i] = ParseKey(skillKeyStrings[i]);
        }
    }

    private KeyCode ParseKey(string keyName)
    {
        try
        {
            // finds the Enum that matches the typed string
            return (KeyCode)System.Enum.Parse(typeof(KeyCode), keyName);
        }
        catch
        {
            Debug.LogError($"KeybindManager: Could not find a key named '{keyName}'. Defaulting to None.");
            return KeyCode.None;
        }
    }

    public void RebindSkill(int index, string newKeyName)
    {
        if (index >= 0 && index < skillKeys.Length)
        {
            skillKeyStrings[index] = newKeyName;
            skillKeys[index] = ParseKey(newKeyName);
        }
    }
}