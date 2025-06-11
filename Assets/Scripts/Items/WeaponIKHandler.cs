// WeaponIKHandler.cs
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class WeaponIKHandler : MonoBehaviour
{
    [Tooltip("Set to true when a weapon with a hand‐anchor is equipped")]
    public bool ikActive = false;

    [Tooltip("The sword's RightHandAnchor transform")]
    public Transform rightHandTarget;

    [Range(0,1)] public float positionWeight = 1f;
    [Range(0,1)] public float rotationWeight = 1f;

    private Animator _anim;

    void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    // Called by Unity right after it evaluates all animation clips
    void OnAnimatorIK(int layerIndex)
    {
        if (_anim == null) return;

        if (ikActive && rightHandTarget != null)
        {
            _anim.SetIKPositionWeight(AvatarIKGoal.RightHand, positionWeight);
            _anim.SetIKRotationWeight(AvatarIKGoal.RightHand, rotationWeight);

            _anim.SetIKPosition   (AvatarIKGoal.RightHand, rightHandTarget.position);
            _anim.SetIKRotation   (AvatarIKGoal.RightHand, rightHandTarget.rotation);
        }
        else
        {
            _anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            _anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        }
    }
}
