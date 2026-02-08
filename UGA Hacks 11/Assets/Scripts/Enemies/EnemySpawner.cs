using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public GameObject left, right, center;
    public List<Color> colors;
    private GameObject[] lanes = new GameObject[3];
    public GameObject player;
    private List<Enemy> enemies;

    public Enemy enemyPrefab;
    public float enemySpeed;
    public int maxEnemies;
    public float spawnDelay;

    private float currentTime;  
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemies = new List<Enemy>();
        lanes[0] = left;
        lanes[1] = center;
        lanes[2] = right;
        currentTime = spawnDelay;
    }

    // Update is called once per frame
    void Update()
    {
        currentTime -= spawnDelay * Time.deltaTime;
        if(currentTime <= 0)
        {
            //SPanw Enemy
            if(enemies.Count < maxEnemies)
            {
                SpawnEnemy();
            }
            currentTime = spawnDelay;
        }
        for(int i = enemies.Count - 1; i >= 0; i--)
        {
            //Debug.Log("Checking enemy " + i);
            if (enemies[i].dead)
            {
                Debug.Log("Delete enemy here");
                Enemy e = enemies[i];
                enemies.RemoveAt(i);
                Destroy(e.gameObject);
            }
        }
    }

    void SpawnEnemy()
    {
        int laneid = Random.Range(0,3);
        Enemy t_enemy = Instantiate(enemyPrefab, lanes[laneid].transform.position, transform.rotation) as Enemy;
        t_enemy.spawn = lanes[laneid];
        t_enemy.speed = enemySpeed;
        t_enemy.player = this.gameObject;
        SpriteRenderer sprt = t_enemy.GetComponent<SpriteRenderer>() as SpriteRenderer;
        sprt.color = colors[Random.Range(0,colors.Count)];
        enemies.Add(t_enemy);
    }
}
