using UnityEngine;

public class Enemy : MonoBehaviour
{
    //Target to walk towards
    public static GameObject player;
    //Where to Spawn
    public GameObject spawn;

    private Vector3 target;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target = player.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
