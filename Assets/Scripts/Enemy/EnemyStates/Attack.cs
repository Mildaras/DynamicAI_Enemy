using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public class Attack : IState
{
    private readonly EnemyRefrences _refs;
    private readonly NavMeshAgent   _agent;
    private readonly Animator       _anim;
    private readonly Transform      _player;

    // melee stats
    private const float ATTACK_RANGE = 2f;
    private const float DAMAGE       = 50f;
    private const float COOLDOWN     = 1f;
    private float       _lastAttack;
    public  bool  IsDone            { get; private set; }

    // sword instance
    private GameObject _swordInstance;

    public Attack(EnemyRefrences refs)
    {
        _refs   = refs;
        _agent  = refs.agent;
        _anim   = refs.animator;
        _player = refs.player;
    }

    public void OnEnter()
    {
        IsDone = false;
        // 1) Spawn and parent the sword
        _swordInstance = Object.Instantiate(
            _refs.swordPrefab,
            _refs.weaponHolder
        );
        _swordInstance.transform.localPosition = Vector3.zero;
        _swordInstance.transform.localRotation = Quaternion.identity;

        // 2) Enable IK to snap the hand to the sword anchor
        var anchor = _swordInstance.transform.Find("RightHandAnchor");
        if (_refs.ikHandler != null && anchor != null)
        {
            _refs.ikHandler.rightHandTarget = anchor;
            _refs.ikHandler.ikActive        = true;
        }

        // 3) Stop moving and trigger your attack animation
        if (_agent != null && _agent.isOnNavMesh && _agent.isActiveAndEnabled)
            _agent.isStopped = true;
        _lastAttack      = Time.time - COOLDOWN;
        _anim?.SetTrigger("attack");
    }

    public void Tick()
    {
        // Face the player
        Vector3 dir = (_player.position - _refs.transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(dir);
            _refs.transform.rotation = Quaternion.Slerp(
                _refs.transform.rotation,
                target,
                Time.deltaTime * 5f
            );
        }

        // If in range and cooled down, deal damage
        float dist = Vector3.Distance(_refs.transform.position, _player.position);
        if (dist <= ATTACK_RANGE && Time.time >= _lastAttack + COOLDOWN)
        {
            _lastAttack = Time.time;
            _anim?.SetTrigger("attack");
            
            var enemy = _refs.GetComponent<Enemy>();
            ActionLogger.Instance?.LogActionWithContext(
                actor:     "Enemy",
                actionType:"Enemy_MeleeAttack",   
                target:     "Player", 
                isHit:      true,
                damage:     DAMAGE,
                distance:   dist,
                actorHealthPercent: enemy?.CurrentHealth / enemy?.maxHealth ?? -1f,
                targetHealthPercent: PlayerData.playerHealth / 100f,
                actorState: "Attacking",
                wasSuccessful: true
            );
            
            PlayerData.takeDamage(DAMAGE);
            IsDone = true; // Attack is done
        }
    }

    public void OnExit()
    {
        IsDone = true; // Mark as done
        // 1) Remove the sword model
        if (_swordInstance != null)
            Object.Destroy(_swordInstance);

        // 2) Turn off IK
        if (_refs.ikHandler != null)
            _refs.ikHandler.ikActive = false;

        // 3) Resume movement and reset animation trigger
        if (_agent != null && _agent.isOnNavMesh && _agent.isActiveAndEnabled)
            _agent.isStopped = false;
        //_anim?.ResetTrigger("attack", a);
        _anim?.ResetTrigger("attack");
    }

    public Color GizmoColor() => Color.red;
}