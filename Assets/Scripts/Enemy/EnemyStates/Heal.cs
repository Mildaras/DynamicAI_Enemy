using UnityEngine;
using UnityEngine.AI;

public class Heal : IState
{
    private readonly EnemyRefrences _refs;
    private readonly NavMeshAgent   _agent;
    private readonly Enemy          _enemy;
    private readonly Animator       _anim;
    private readonly GameObject     _auraPrefab;

    private GameObject _auraInstance;
    private Vector3    _origAuraScale;

    private float _timer;
    private const float DURATION    = 3f;
    private float HEAL_AMOUNT = 200f;
    public  bool  IsDone            { get; private set; }

    public Heal(EnemyRefrences refs)
    {
        _refs       = refs;
        _agent      = refs.agent;
        _enemy      = refs.GetComponent<Enemy>();
        _anim       = refs.animator;
        _auraPrefab = refs.healAuraPrefab; // assign in Inspector
    }

    public void OnEnter()
    {
        _timer   = 0f;
        IsDone   = false;

        if (_agent != null)
            _agent.isStopped = true;

        _anim?.SetBool("isHealing", true);

        // Spawn the healing aura at enemy center
        if (_auraPrefab != null)
        {
            _auraInstance = Object.Instantiate(
                _auraPrefab,
                _refs.transform.position,
                Quaternion.identity
            );
            // Parent to enemy so it follows position
            _auraInstance.transform.SetParent(_refs.transform, true);
            _origAuraScale = _auraInstance.transform.localScale;
        }
    }

    public void Tick()
    {
        _timer += Time.deltaTime;

        // Shrink the aura over the duration
        if (_auraInstance != null)
        {
            float t = 1f - Mathf.Clamp01(_timer / DURATION);
            _auraInstance.transform.localScale = _origAuraScale * t;
        }

        if (_timer >= DURATION)
        {
            _anim?.SetBool("isHealing", false);
            _anim?.SetTrigger("healRelease");
            _enemy.Heal(HEAL_AMOUNT);
            HEAL_AMOUNT = 0f; // Prevent healing again
            IsDone = true;
        }
    }

    public void OnExit()
    {
        if (_agent != null)
            _agent.isStopped = false;

        _anim?.SetBool("isHealing", false);
        HEAL_AMOUNT = 200f; 

        if (_auraInstance != null)
            Object.Destroy(_auraInstance);
    }

    public Color GizmoColor() => Color.green;
}
