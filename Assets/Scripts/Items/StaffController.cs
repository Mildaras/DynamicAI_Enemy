using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class StaffController : MonoBehaviour
{
    [Header("Staff Stats")]
    [SerializeField] private float damage   = 20f;
    [SerializeField] private float cooldown = 1f;
    public static event Action OnStaffFired;

    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab; // must have HomingProjectile
    [SerializeField] private Transform  muzzlePoint;
    [SerializeField] private float      projectileSpeed    = 25f;
    [SerializeField] private float      projectileLifeTime = 5f;

    [Header("Aiming")]
    [SerializeField] private Camera     cam;             // assign or tagged MainCamera
    [SerializeField] private LayerMask  aimLayerMask;    // e.g. Default | Enemy
    [SerializeField] private float      maxAimDistance    = 100f;
    [SerializeField] private float      maxIndicatorDistance = 20f;

    [Header("Indicator")]
    [SerializeField] private GameObject aimIndicatorPrefab;
    [SerializeField] private float      indicatorYOffset = 0.05f;

    private float           _nextAllowedShot = 0f;
    private GameObject      _indicatorInst;
    private Projectile _homingProj; 

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) Debug.LogError("StaffController: no camera assigned or tagged MainCamera!");
    }

    void Update()
    {
        // ── Press: spawn indicator & projectile ─────────────────────────────
        if (Input.GetMouseButtonDown(0) && Time.time >= _nextAllowedShot)
        {
            // 1) Create the indicator
            if (aimIndicatorPrefab != null)
                _indicatorInst = Instantiate(aimIndicatorPrefab);

            // 2) Fire the projectile immediately
            var projGO = Instantiate(
                projectilePrefab,
                muzzlePoint.position,
                Quaternion.identity
            );
            _homingProj = projGO.GetComponent<Projectile>();
            if (_homingProj == null)
                Debug.LogError("Projectile prefab needs a HomingProjectile component!");
            else
                // point it at the initial indicator position (we’ll update it next frame)
                _homingProj.Initialize(
                    _indicatorInst.transform,
                    damage,
                    projectileSpeed,
                    projectileLifeTime
                );

            _nextAllowedShot = Time.time + cooldown;
            OnStaffFired?.Invoke();
        }

        // ── Hold: update indicator & homing target ───────────────────────────
        if (Input.GetMouseButton(0) && _indicatorInst != null)
        {
            // reposition the indicator
            UpdateIndicatorPosition();

            // feed its position to the homing script
            if (_homingProj != null)
                _homingProj.UpdateTarget(_indicatorInst.transform.position);
        }

        // ── Release: destroy indicator ───────────────────────────────────────
        if (Input.GetMouseButtonUp(0) && _indicatorInst != null)
        {
            Destroy(_indicatorInst);
            _indicatorInst = null;
            _homingProj    = null;  // projectile will keep using the last target pos
        }
    }

    private void UpdateIndicatorPosition()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        float hitDistance;
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimLayerMask))
            hitDistance = hit.distance;
        else
            hitDistance = maxAimDistance;

        hitDistance = Mathf.Min(hitDistance, maxIndicatorDistance);
        Vector3 point = ray.origin + ray.direction * hitDistance;
        _indicatorInst.transform.position = point + Vector3.up * indicatorYOffset;
    }
}
