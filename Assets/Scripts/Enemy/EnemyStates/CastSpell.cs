using UnityEngine;
using UnityEngine.AI;

public enum SpellType
{
    Fast,
    Medium,
    Slow
}

public class CastSpell : IState
{
    private readonly EnemyRefrences _refs;
    private readonly NavMeshAgent   _agent;
    private readonly Animator       _anim;
    private readonly Transform      _castPoint;

    private readonly GameObject     _chargePrefab;
    private readonly GameObject     _spellPrefab;
    private readonly float          _castTime;
    private readonly float          _spellSpeed;
    private readonly float          _spellDamage;
    private readonly float          _spellLifeTime;
    private readonly string         _spellName;

    private float      _timer;
    private bool       _hasFired;
    private GameObject _chargeInstance;
    private Vector3    _origChargeScale;
    private Vector3    _targetChargeScale;



    public bool IsDone { get; private set; }

    public CastSpell(EnemyRefrences refs, SpellType type)
    {
        _refs      = refs;
        _agent     = refs.agent;
        _anim      = refs.animator;
        _castPoint = refs.castPoint;

        switch (type)
        {
            case SpellType.Fast:
                _chargePrefab    = refs.fastChargePrefab;
                _spellPrefab     = refs.fastSpellPrefab;
                _castTime        = refs.fastCastTime;
                _spellSpeed      = refs.fastSpellSpeed;
                _spellDamage     = refs.fastSpellDamage;
                _spellLifeTime   = refs.fastSpellLifeTime;
                _spellName = "Fast";
                break;

            case SpellType.Medium:
                _chargePrefab    = refs.medChargePrefab;
                _spellPrefab     = refs.medSpellPrefab;
                _castTime        = refs.medCastTime;
                _spellSpeed      = refs.medSpellSpeed;
                _spellDamage     = refs.medSpellDamage;
                _spellLifeTime   = refs.medSpellLifeTime;
                _spellName = "Medium";
                break;

            case SpellType.Slow:
                _chargePrefab    = refs.slowChargePrefab;
                _spellPrefab     = refs.slowSpellPrefab;
                _castTime        = refs.slowCastTime;
                _spellSpeed      = refs.slowSpellSpeed;
                _spellDamage     = refs.slowSpellDamage;
                _spellLifeTime   = refs.slowSpellLifeTime;
                _spellName = "Slow";
                break;

            default:
                // fallback to medium
                _chargePrefab    = refs.medChargePrefab;
                _spellPrefab     = refs.medSpellPrefab;
                _castTime        = refs.medCastTime;
                _spellSpeed      = refs.medSpellSpeed;
                _spellDamage     = refs.medSpellDamage;
                _spellLifeTime   = refs.medSpellLifeTime;
                _spellName = "Medium";
                break;
        }
    }

    public void OnEnter()
    {
        _timer     = 0f;
        _hasFired  = false;
        IsDone     = false;

        if (_agent != null)
            _agent.isStopped = true;

        _anim?.SetBool("isCasting", true);

        if (_chargePrefab != null && _castPoint != null)
        {
            _chargeInstance  = Object.Instantiate(
                _chargePrefab,
                _castPoint.position,
                Quaternion.identity
            );
            _origChargeScale = _chargeInstance.transform.localScale;
            _targetChargeScale = _origChargeScale * 3f;
        }
    }

    public void Tick()
    {
        _timer += Time.deltaTime;

        if (_chargeInstance != null)
        {
            float t = Mathf.Clamp01(_timer / _castTime);
            _chargeInstance.transform.localScale =
                Vector3.Lerp(_origChargeScale, _targetChargeScale, t);
        }

        if (!_hasFired && _timer >= _castTime)
        {
            _anim?.SetBool("isCasting", false);
            _anim?.SetTrigger("castRelease");
            SpellProjectile projectile = null;

            Vector3 targetPos = _refs.player.position;
            if (_spellPrefab != null && _castPoint != null)
            {
                var go = Object.Instantiate(
                    _spellPrefab,
                    _castPoint.position,
                    Quaternion.identity
                );
                var proj = go.GetComponent<SpellProjectile>();
                proj?.Initialize(
                    targetPos,
                    _spellSpeed,
                    _spellDamage,
                    _spellLifeTime
                );
                projectile = proj;
            }

            if (_chargeInstance != null)
                Object.Destroy(_chargeInstance);

            _hasFired = true;
            IsDone    = true;
        }
    }

    public void OnExit()
    {
        ActionLogger.Instance.LogAction(
            actor:     "Enemy",
            actionType:"ShotSpell",
            target:    "Player",
            isHit:     true,
            damage:    0f,
            distance:  0f
        );
        _hasFired = false;
        IsDone    = false;
        if (_agent != null)
            _agent.isStopped = false;

        _anim?.ResetTrigger("castRelease");
        _anim?.SetBool("isCasting", false);

        if (_chargeInstance != null)
        {
            Object.Destroy(_chargeInstance);
        }
    }

    public Color GizmoColor() => Color.magenta;
}
