using UnityEngine;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    public enum EnemyType {circle=0, square=1, triangle=2, star=3 };
    public static List<Color> colors;
    public enum Lane {none=0, left=1, center=2, right=3};

    //Target to walk towards
    public GameObject player;
    //Where to Spawn
    public GameObject spawn;
    public float speed;
    public bool dead;
    public bool dying;
    //Type and size
    public EnemyType type;
    public Lane lane;
    private Vector3 target;

    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Collider2D unitCollider;

    //public Color spriteColor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target = player.transform.position;
        dead = false;
        dying = false;
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        unitCollider = GetComponent<Collider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!dying)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        }
        
    }

// Call this when collision occurs
    public void InitiateSelfDestruct()
    {
        if (dying) return; // Prevent double-triggering

        dying = true;
        
        spriteRenderer.sprite = null;
        // Disable tactical presence but keep object for animation
        unitCollider.enabled = false; 
        
        // Trigger the 4-frame nuke
        anim.SetTrigger("PlayExplosion"); 
    }

    // This method is called by the Animation Event at the end of the clip
    public void OnAnimationComplete()
    {
        Debug.Log("Animation done.");
        dead = true;
        
        // Optional: Remove from the battlefield entirely
        // Destroy(gameObject, 0.1f);
    }

    // This triggers when another collider touches this one
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Collided with " + collision.gameObject.tag);
        // Tactical Check: Only die if we hit something specific (like a "Bullet" or "Player")
        Debug.Log(collision.gameObject.tag == "Player");
        if (collision.gameObject.tag == "Player")
        {
            Debug.Log("Destroying...");
            InitiateSelfDestruct();
        }
    }
}
