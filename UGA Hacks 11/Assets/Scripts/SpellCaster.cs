using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SpellCaster : MonoBehaviour
{
    [SerializeField] private GameObject spellPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spellSpeed = 10f;
    [SerializeField] private int spacingDelayMs = 300;
    [SerializeField] private List<Transform> lanes;

    // The Active Fleet Registry
    private List<SpellProjectile> _activeSpells = new List<SpellProjectile>();

    [SerializeField] private Color[] elementColors = { 
        Color.white, Color.red, Color.blue, Color.green, Color.yellow, Color.magenta 
    };

    void Update()
    {
        // Battlefield Cleanup: Sweep for dead projectiles
        for (int i = _activeSpells.Count - 1; i >= 0; i--)
        {
            if (_activeSpells[i] == null || _activeSpells[i].IsDead)
            {
                if (_activeSpells[i] != null) Destroy(_activeSpells[i].gameObject);
                _activeSpells.RemoveAt(i);
            }
        }
    }

    public async void CastSequence(byte[] sequence, byte direction)
    {
        if (direction >= lanes.Count) return;
        Transform targetLane = lanes[direction];

        foreach (byte shapeClass in sequence)
        {
            DeploySpell(shapeClass, targetLane);
            await Task.Delay(spacingDelayMs);
        }
    }

    private void DeploySpell(byte shapeClass, Transform target)
    {
        GameObject go = Instantiate(spellPrefab, spawnPoint.position, Quaternion.identity);
        SpellProjectile projectile = go.GetComponent<SpellProjectile>();

        Color spellColor = (shapeClass < elementColors.Length) ? elementColors[shapeClass] : Color.white;
        
        projectile.Initialize(target, spellSpeed, spellColor, shapeClass);
        
        // Add to Registry
        _activeSpells.Add(projectile);
    }
}