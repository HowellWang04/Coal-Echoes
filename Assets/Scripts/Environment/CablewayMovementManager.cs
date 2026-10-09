using System;
using UnityEngine;

public class CablewayMovementManager : MonoBehaviour
{
    [SerializeField] private bool startAutomatically = true;
    [SerializeField, Min(0f)] private float speed = 1f;
    [SerializeField] private Transform[] buckets = Array.Empty<Transform>();
    [SerializeField, HideInInspector] private Vector3[] routePoints = Array.Empty<Vector3>();

    private Vector3[] worldPoints;
    private float[] cumulativeLengths;
    private float[] startingDistances;
    private float travelDistance;
    private bool initialized;

    public bool IsMoving { get; private set; }
    public float Speed => speed;
    public float RouteLength => initialized ? cumulativeLengths[worldPoints.Length] : 0f;
    public Transform[] Buckets => buckets;
    public bool HasConfiguration => routePoints.Length >= 3 && buckets.Length > 0
        && Array.TrueForAll(buckets, bucket => bucket != null);

    private void Start()
    {
        if (startAutomatically) StartMovement();
    }

    private void Update()
    {
        AdvanceMovement(Time.deltaTime);
    }

    private void OnDisable()
    {
        StopMovement();
    }

    public void StartMovement()
    {
        if (!initialized && !PrepareMovement()) return;
        IsMoving = true;
    }

    public void StopMovement()
    {
        IsMoving = false;
    }

    private void AdvanceMovement(float deltaTime)
    {
        if (IsMoving) SampleMovement(travelDistance + speed * deltaTime);
    }

    // Cache the closed route and project each bucket's authored position onto it.
    // The editor preview uses the same sampling code as runtime movement.
    public bool PrepareMovement()
    {
        initialized = false;
        if (!HasConfiguration)
        {
            Debug.LogError("Cableway movement needs a closed route and assigned buckets.", this);
            return false;
        }

        worldPoints = new Vector3[routePoints.Length];
        cumulativeLengths = new float[routePoints.Length + 1];
        for (int i = 0; i < routePoints.Length; i++)
            worldPoints[i] = transform.TransformPoint(routePoints[i]);

        for (int i = 0; i < worldPoints.Length; i++)
            cumulativeLengths[i + 1] = cumulativeLengths[i]
                + Vector3.Distance(worldPoints[i], worldPoints[(i + 1) % worldPoints.Length]);

        startingDistances = new float[buckets.Length];
        for (int i = 0; i < buckets.Length; i++)
            startingDistances[i] = FindNearestDistance(buckets[i].position);

        travelDistance = 0f;
        initialized = true;
        return true;
    }

    public void SampleMovement(float distance)
    {
        if (!initialized && !PrepareMovement()) return;
        travelDistance = Mathf.Repeat(distance, RouteLength);
        for (int i = 0; i < buckets.Length; i++)
        {
            float bucketDistance = startingDistances[i] + travelDistance;
            Vector3 position = PositionAtDistance(bucketDistance);
            Vector3 direction = PositionAtDistance(bucketDistance + 0.25f)
                - PositionAtDistance(bucketDistance - 0.25f);
            direction.y = 0f;
            buckets[i].SetPositionAndRotation(position, Quaternion.LookRotation(direction, Vector3.up));
        }
    }

    private Vector3 PositionAtDistance(float distance)
    {
        distance = Mathf.Repeat(distance, cumulativeLengths[worldPoints.Length]);
        if (distance >= cumulativeLengths[worldPoints.Length]) distance = 0f;
        int index = Array.BinarySearch(cumulativeLengths, distance);
        if (index < 0) index = ~index - 1;
        float t = (distance - cumulativeLengths[index])
            / (cumulativeLengths[index + 1] - cumulativeLengths[index]);
        return Vector3.Lerp(worldPoints[index], worldPoints[(index + 1) % worldPoints.Length], t);
    }

    private float FindNearestDistance(Vector3 position)
    {
        float closestSquaredDistance = float.PositiveInfinity;
        float distance = 0f;
        for (int i = 0; i < worldPoints.Length; i++)
        {
            Vector3 segment = worldPoints[(i + 1) % worldPoints.Length] - worldPoints[i];
            float t = Mathf.Clamp01(Vector3.Dot(position - worldPoints[i], segment) / segment.sqrMagnitude);
            float squaredDistance = (position - (worldPoints[i] + segment * t)).sqrMagnitude;
            if (squaredDistance >= closestSquaredDistance) continue;
            closestSquaredDistance = squaredDistance;
            distance = cumulativeLengths[i] + t * (cumulativeLengths[i + 1] - cumulativeLengths[i]);
        }
        return distance;
    }
}
