using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;                           // Needed for NavMeshAgent

public class Enemy : MonoBehaviour
{
    public int health = 100;                     // Enemy health
    private Rigidbody rb;                        // Rigidbody reference
    private Renderer rend;                       // Renderer reference
    private Material originalMat;                // Original material for blinking effect
    public Material hitMat;                      // Material to show when enemy is hit

    private NavMeshAgent agent;                  // NavMeshAgent for pathfinding
    public int currentPointIndex = 0;            // Current patrol point index
    public Vector3 currentTarget;                // Current patrol target
    public float positionThreshold = 2f;         // Distance considered "reached" for patrol point
    public float idleTime = 3f;                  // Idle duration at patrol points
    public float attackDistance = 5f;            // Distance at which enemy attacks
    public float maxVisionDistance = 15f;        // How far enemy can see the player
    public float minimumChasingHealth = 30f;     // Below this, enemy will avoid player
    public Transform[] patrolPoints;             // Array of patrol points
    private float idleTimeCounter;               // Counter for idle duration
    private Transform playerTransform;           // Reference to player
    private bool canSeePlayer;                   // Flag to indicate if player is visible
    private Vector3 lastKnownPlayerPosition;     // Last position where player was seen

    // Enemy states
    public enum State { Idle, Patrolling, Chasing, Attacking }
    public State state = State.Idle;             // Default state is Idle

    void Start()
    {
        //currentPointIndex = 0;
        //currentTarget = patrolPoints[currentPointIndex].position;

        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();    // Visual feedback (blink on hit, etc.)
        originalMat = rend.material;

        agent = GetComponent<NavMeshAgent>(); // NAVMESH: agent used for pathfinding & movement

        // Player reference (chase target)
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        // Patrol points setup
        GameObject patrolParent = GameObject.FindGameObjectWithTag("PatrolPoint");
        patrolPoints = patrolParent
            .GetComponentsInChildren<Transform>()
            .Where(t => t != patrolParent.transform)
            .ToArray();

        idleTimeCounter = idleTime;
    }


    void Update()
    {
        LookForPlayer(); // Check if player is visible

        switch (state)
        {
            case State.Idle:
                Idle();
                break;
            case State.Patrolling:
                Patrolling();
                break;
            case State.Chasing:
                Chasing();
                break;
            case State.Attacking:
                Attacking();
                break;
        }

        // Stop rigidbody from interfering with NavMeshAgent
        rb.linearVelocity = Vector3.zero;

        // Always look at player if seen
        LookAtPlayer();

        SetLastKnownPlayerPosition();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!this.enabled) return; // If enemy is dead, do nothing
        if (collision.gameObject.CompareTag("damage"))
        {
            health -= 10;           // Lose 10 health per hit
            if (health <= 0)
            {
                Die();              // Call Die function
            }
            else
            {
                StartCoroutine(Blink()); // Blink to indicate damage
            }
        }
    }

    private void Die()
    {
        if (!this.enabled) return;         // Stop all actions if dead
        rb.freezeRotation = false;         // Allow enemy to tip over
        transform.rotation = Quaternion.Euler(
            transform.rotation.x,
            transform.rotation.y,
            transform.rotation.z + 5f // Slight rotation on Z so enemy tips over
        );
        this.enabled = false;              // Disable script
    }

    IEnumerator Blink()
    {
        rend.material = hitMat;            // Show hit material
        yield return new WaitForSeconds(0.1f);
        rend.material = originalMat;       // Revert to original
    }

    private void Idle()
    {
        agent.ResetPath();                  // Stand still
        idleTimeCounter -= Time.deltaTime;
        if (idleTimeCounter <= 0f)
        {
            state = State.Patrolling;      // Switch to patrol after idle
            idleTimeCounter = idleTime;    // Reset idle timer
        }
    }

    private void Patrolling()
    {
        if (Vector3.Distance(transform.position, currentTarget) < positionThreshold)
        {
            float chance = Random.Range(0f, 100f);
            if (chance < 10f)
            {
                state = State.Idle;         // 10% chance to idle at patrol point
                return;
            }

            currentPointIndex++;
            currentTarget = patrolPoints[currentPointIndex % patrolPoints.Length].position;
        }
        else
        {
            agent.SetDestination(currentTarget); // Move toward current target
        }
    }

    private void Attacking()
    {
        idleTimeCounter = idleTime;
        agent.ResetPath();                  // Stand still while "attacking"
        // Shooting logic will be added in next episode

        if (Vector3.Distance(transform.position, playerTransform.position) > attackDistance || !canSeePlayer)
        {
            if (health < minimumChasingHealth)
            {
                state = State.Patrolling;  // Avoid player if low health
            }
            else
            {
                state = State.Chasing;     // Otherwise chase
            }
        }
    }

    private void Chasing()
    {
        idleTimeCounter = idleTime;
        agent.SetDestination(lastKnownPlayerPosition); // Move towards where player was last seen

        if (health < minimumChasingHealth)
        {
            state = State.Patrolling; // If health is low, stop chasing
        }
        else if (Vector3.Distance(transform.position, playerTransform.position) <= attackDistance && canSeePlayer)
        {
            state = State.Attacking;   // Close enough and can see player, switch to attack
        }
        else if (Vector3.Distance(transform.position, playerTransform.position) > maxVisionDistance)
        {
            state = State.Patrolling; // Player too far, resume patrol
        }
        else if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < positionThreshold && !canSeePlayer)
        {
            state = State.Patrolling; // Reached last known position but can't see player
        }
    }

    private void LookAtPlayer()
    {
        if (canSeePlayer)
        {
            // Rotate to face player, only on XZ plane
            Vector3 lookPos = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
            transform.LookAt(lookPos);
        }
    }

    private void SetLastKnownPlayerPosition()
    {
        if (canSeePlayer)
        {
            lastKnownPlayerPosition = playerTransform.position;
        }
    }

    private void LookForPlayer()
    {
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToPlayer, out hit, maxVisionDistance))
        {
            canSeePlayer = hit.transform == playerTransform; // True if ray hits player directly

            if (canSeePlayer && state != State.Attacking)
            {
                state = State.Chasing; // Begin chasing if player visible
            }

        }

        SetLastKnownPlayerPosition();
    }
}
