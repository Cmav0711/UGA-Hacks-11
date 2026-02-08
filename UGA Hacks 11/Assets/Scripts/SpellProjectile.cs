using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    private Transform _targetLane;
    private float _speed;
    private bool _initialized = false;
    
    public byte ShapeClass;
    public bool IsDead = false;

    // Tactical Asset: The Sprite Renderer
    [SerializeField] private SpriteRenderer _renderer;

    public void Initialize(Transform target, float speed, Color elementColor, byte shapeClass)
    {
        _targetLane = target;
        _speed = speed;
        ShapeClass = shapeClass;
        GetComponent<SpriteRenderer>().color = elementColor;
        // Apply the elemental color to the sprite
        if (_renderer != null)
        {
            
        }

        _initialized = true;
    }

    void Update()
    {
        // If not initialized, dead, or target is lost, hold position
        if (!_initialized || IsDead || _targetLane == null) return;

        // MoveTowards is safer than manual addition for reaching specific targets
        transform.position = Vector3.MoveTowards(
            transform.position, 
            _targetLane.position, 
            _speed * Time.deltaTime
        );

        // Check if we reached the target lane
        if (Vector3.Distance(transform.position, _targetLane.position) < 0.1f)
        {
            IsDead = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Log EVERY trigger hit to find the culprit
        Debug.Log($"<color=yellow>[SENSORS]</color> Hit: {collision.gameObject.name} Tag: {collision.tag}");

        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null && (int)enemy.type == ShapeClass)
            {
                IsDead = true;
                enemy.InitiateSelfDestruct();
            }
        }
    }
}