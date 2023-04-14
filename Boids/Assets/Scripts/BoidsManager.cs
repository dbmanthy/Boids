using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidsManager : MonoBehaviour
{
    public int boidCount;
    public bool isVisualized;

    public GameObject boidPrefab;

    [Header("Flock Behavior")]
    public bool seperation;
    public bool alignment;
    public bool cohesion;
    public bool hasTarget;

    [Header("Boid Behavior")]
    public float moveSpeed;
    public float turnRadius;
    public float viewAngle;
    public float viewRadius;

    List<GameObject> boids = new List<GameObject>();
    Vector2 halfscreenWidthInWorldUnits;

    void Start()
    {
        halfscreenWidthInWorldUnits = new Vector2(Camera.main.aspect * Camera.main.orthographicSize, Camera.main.orthographicSize);

        for (int i = 0; i < boidCount; i++)
        {
            GameObject boid = Instantiate(boidPrefab, new Vector2(Random.Range(-halfscreenWidthInWorldUnits.x, halfscreenWidthInWorldUnits.x), Random.Range(-halfscreenWidthInWorldUnits.y, halfscreenWidthInWorldUnits.y)), Quaternion.Euler(0f, 0f, Random.Range(0, 360)));
            boids.Add(boid);
        }
    }
}
