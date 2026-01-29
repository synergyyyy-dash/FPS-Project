using UnityEngine;
using UnityEngine.InputSystem; // Input System (Send Messages)

public class Pickup : MonoBehaviour
{
    public Material highlightMaterial;       // material shown when looked at
    private Material[] originalMaterials;    // original materials to restore
    private MeshRenderer[] meshRenderers;    // all renderers in this object
    public GameObject weaponPrefab;          // equippable weapon prefab
    public float lookRange = 3f;             // max raycast distance

    private bool isLookedAt = false;         // true while aiming at this item
    private Camera playerCam;                // player's camera
    private PlayerShooting player;           // player shooting component

    void Start()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>(); // gather renderers
        originalMaterials = new Material[meshRenderers.Length];  // cache originals

        for (int i = 0; i < meshRenderers.Length; i++)
            originalMaterials[i] = meshRenderers[i].material;    // store material

        player = FindFirstObjectByType<PlayerShooting>();        // get PlayerShooting
        playerCam = player.GetComponentInChildren<Camera>();     // get player camera
    }

    void Update()
    {
        // ray from camera forward (what the player is looking at)
        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);

        // check hit within lookRange
        if (Physics.Raycast(ray, out RaycastHit hit, lookRange))
        {
            // is the hit object THIS pickup?
            if (hit.collider.GetComponentInParent<Pickup>() == this)
            {
                if (!isLookedAt) SetLookedAt(true);              // turn highlight on
                return;                                          // stop checking further
            }
        }

        // no longer looking: remove highlight
        if (isLookedAt) SetLookedAt(false);
    }

    void SetLookedAt(bool lookedAt)
    {
        isLookedAt = lookedAt;                                   // update flag
        if (lookedAt)
        {
            foreach (MeshRenderer mr in meshRenderers)
                mr.material = highlightMaterial;                 // apply highlight
        }
        else
        {
            for (int i = 0; i < meshRenderers.Length; i++)
                meshRenderers[i].material = originalMaterials[i];// restore originals
        }
    }

    public void OnPickUp()                                       // called by Input action "PickUp"
    {
        if (!isLookedAt) return;                                 // only when aiming at item

        player.OnDrop();              // remove current gun

        // spawn weapon under GunHolder (view model)
        GameObject newWeapon = Instantiate(weaponPrefab, player.gunHolder);
        newWeapon.transform.localPosition = Vector3.zero;        // reset local pos
        newWeapon.transform.localRotation = Quaternion.identity; // reset local rot

        player.gun = newWeapon.GetComponent<Gun>();              // cache new Gun
        Destroy(gameObject);          // remove dropped item
    }


}
