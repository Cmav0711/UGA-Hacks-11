using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpellSequencer : MonoBehaviour
{
    [Header("Intelligence Assets")]
    [SerializeField] private List<Sprite> classSprites; // 0: Circle, 1: Square, 2: Triangle
    [SerializeField] private Transform stackUIContainer; // Where to show the sprites
    [SerializeField] private GameObject spritePrefab; // Simple UI Image prefab

    private Stack<byte> _classStack = new Stack<byte>();
    private List<GameObject> _visualStack = new List<GameObject>();

    public GameObject player;
    private SpellCaster caster;
    public void AddToStack(byte shapeClass)
    {
        _classStack.Push(shapeClass);
        
        // Update Visuals
        GameObject newIcon = Instantiate(spritePrefab, stackUIContainer);
        newIcon.GetComponent<Image>().sprite = classSprites[shapeClass];
        _visualStack.Add(newIcon);
        caster = player.GetComponent<SpellCaster>();
    }

    public void FinalizeSequence(byte direction)
    {
        if (_classStack.Count == 0) return;

        // Convert stack to a readable format for the spell logic
        byte[] sequence = _classStack.ToArray();
        System.Array.Reverse(sequence); // Stack is LIFO, reverse to get original order
        Debug.Log("Executing Spell: " + direction);
        ExecuteSpell(sequence, direction);
        ClearStack();
    }

    private void ExecuteSpell(byte[] sequence, byte direction)
    {
        string spellName = string.Join("-", sequence);
        Debug.Log($"DEPLOYING SPELL: {spellName} toward Direction: {direction}");

        // Tactical Logic Examples:
        // 0-0-1 + Dir 1 = Fireball Left
        // 2-1-0 + Dir 2 = Shield Center
        caster.CastSequence(sequence, direction);
    }

    private void ClearStack()
    {
        _classStack.Clear();
        foreach (var icon in _visualStack) Destroy(icon);
        _visualStack.Clear();
    }
}