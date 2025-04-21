using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a potential target for missiles and HUD tracking.
/// Provides access to position, velocity, and missile tracking information.
/// </summary>
public class Target : MonoBehaviour
{
    [SerializeField]
    new string name;

    /// <summary>
    /// The display name of the target, used in the HUD.
    /// </summary>
    public string Name
    {
        get
        {
            return name;
        }
    }

    /// <summary>
    /// Returns the current position of the target based on its Rigidbody or transform.
    /// </summary>
    public Vector3 Position
    {
        get
        {
            return rigidbody != null ? rigidbody.position : transform.position;
        }
    }

    /// <summary>
    /// Returns the current velocity of the target based on its Rigidbody.
    /// </summary>
    public Vector3 Velocity
    {
        get
        {
            return rigidbody != null ? rigidbody.linearVelocity : Vector3.zero;
        }
    }

    /// <summary>
    /// Returns the associated Plane component if available.
    /// </summary>
    public Plane Plane { get; private set; }

    new Rigidbody rigidbody;

    List<Missile> incomingMissiles;
    const float sortInterval = 0.5f;
    float sortTimer;

    /// <summary>
    /// Initializes the Rigidbody and Plane components and missile list.
    /// </summary>
    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        Plane = GetComponent<Plane>();

        incomingMissiles = new List<Missile>();
    }

    /// <summary>
    /// Runs every fixed frame to re-sort incoming missile list periodically.
    /// </summary>
    void FixedUpdate()
    {
        sortTimer = Mathf.Max(0, sortTimer - Time.fixedDeltaTime);

        if (sortTimer == 0)
        {
            SortIncomingMissiles();
            sortTimer = sortInterval;
        }
    }

    /// <summary>
    /// Sorts incoming missiles by distance to this target.
    /// </summary>
    void SortIncomingMissiles()
    {
        var position = Position;

        if (incomingMissiles.Count > 0)
        {
            incomingMissiles.Sort((Missile a, Missile b) =>
            {
                var distA = Vector3.Distance(a.Rigidbody.position, position);
                var distB = Vector3.Distance(b.Rigidbody.position, position);
                return distA.CompareTo(distB);
            });
        }
    }

    /// <summary>
    /// Returns the closest incoming missile currently tracking this target.
    /// </summary>
    /// <returns>The closest incoming Missile object or null.</returns>
    public Missile GetIncomingMissile()
    {
        if (incomingMissiles.Count > 0)
        {
            return incomingMissiles[0];
        }

        return null;
    }

    /// <summary>
    /// Adds or removes a missile from the incoming missile list.
    /// </summary>
    /// <param name="missile">The missile to add or remove.</param>
    /// <param name="value">True to add, false to remove.</param>
    public void NotifyMissileLaunched(Missile missile, bool value)
    {
        if (value)
        {
            incomingMissiles.Add(missile);
            SortIncomingMissiles();
        }
        else
        {
            incomingMissiles.Remove(missile);
        }
    }
}
