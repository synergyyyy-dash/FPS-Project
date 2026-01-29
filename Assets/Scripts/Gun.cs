using UnityEngine;
using System.Collections;

public class Gun : MonoBehaviour
{
    // Firing & reload settings
    public float reloadTime = 1f;
    public float fireRate = 0.15f;
    public int maxSize = 20;
    public GameObject droppedWeapon;

    // Bullet & spawn point
    public GameObject bullet;
    public Transform bulletSpawnPoint;

    public float recoilDistance = 0.1f;
    public float recoilSpeed = 15f;
    public GameObject weaponFlash;

    // State
    private int currentAmmo;
    private bool isReloading = false;
    private float nextTimeToFire = 0f;

    // Initial transforms and reload rotation offsets
    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private Vector3 reloadRotationOffset = new Vector3(66f, 50f, 50f);

    void Start()
    {
        // Cache starting pose and fill magazine
        currentAmmo = maxSize;
        initialRotation = transform.localRotation;
        initialPosition = transform.localPosition;
    }

    // Called by PlayerShooting while holding click
    public void Shoot()
    {
        // Block firing if reloading or waiting for fire-rate
        if (isReloading) return;
        if (Time.time < nextTimeToFire) return;

        // Auto-start reload when empty
        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        // Schedule next shot and consume ammo
        nextTimeToFire = Time.time + fireRate;
        currentAmmo--;

        // Spawn bullet and muzzle flash at the barrel
        Instantiate(bullet, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        Instantiate(weaponFlash, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

        StopCoroutine(nameof(Recoil));
        StartCoroutine(Recoil());
    }

    // Reload animation: rotate to offset, then back
    IEnumerator Reload()
    {
        isReloading = true;

        Quaternion targetRotation = Quaternion.Euler(initialRotation.eulerAngles + reloadRotationOffset);

        float halfReload = reloadTime / 2f;
        float t = 0f;

        // Rotate from initial → target
        while (t < halfReload)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(
                initialRotation, targetRotation, t / halfReload);
            yield return null;
        }

        // Rotate from target → initial
        t = 0f;
        while (t < halfReload)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(
                targetRotation, initialRotation, t / halfReload);
            yield return null;
        }

        // Refill magazine
        currentAmmo = maxSize;
        isReloading = false;
    }

    // Called by PlayerShooting when pressing R
    public void TryReload()
    {
        // Skip if already full or already reloading
        if (isReloading || currentAmmo == maxSize) return;
        StartCoroutine(Reload());
    }

    IEnumerator Recoil()
    {
        Vector3 recoilTarget = initialPosition + new Vector3(0f, 0f, -recoilDistance);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * recoilSpeed;
            transform.localPosition = Vector3.Lerp(initialPosition, recoilTarget, t);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * recoilSpeed;
            transform.localPosition = Vector3.Lerp(recoilTarget, initialPosition, t);
            yield return null;
        }

        transform.localPosition = initialPosition;
    }

    public void Drop()
    {
        Instantiate(droppedWeapon, transform.position, transform.rotation);

        Destroy(gameObject);
    }

}
