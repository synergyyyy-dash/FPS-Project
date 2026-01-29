using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health = 100;          // Enemy health
    public Material hitMat;           // Material to flash when hit

    private Rigidbody rb;             // Reference to Rigidbody
    private Renderer rend;            // Reference to Renderer
    private Material originalMaterial;// Store original material to restore

    void Start()
    {
        rb = GetComponent<Rigidbody>();       // Get Rigidbody component
        rend = GetComponent<Renderer>();      // Get Renderer component
        originalMaterial = rend.material;     // Store original material
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "damage") // Check if hit by bullet
        {
            health -= 10;                     // Reduce health by 10

            if (health <= 0)
            {
                Die();                        // Call Die() if health is 0 or less
            }
            else
            {
                StartCoroutine(Blink());       // Flash material to show hit
            }
        }
    }

    void Die()
    {
        if (!this.enabled) return;            // If already dead, do nothing

        rb.freezeRotation = false;            // Allow enemy to fall over

        transform.rotation = Quaternion.Euler(
            transform.rotation.x,
            transform.rotation.y,
            transform.rotation.z + 5         // Slight rotation on Z so it tips over
        );

        this.enabled = false;                 // Disable script so no further actions
    }

    IEnumerator Blink()
    {
        rend.material = hitMat;               // Switch to hit material
        yield return new WaitForSeconds(0.1f);// Wait 0.1 seconds
        rend.material = originalMaterial;     // Restore original material
    }
}
