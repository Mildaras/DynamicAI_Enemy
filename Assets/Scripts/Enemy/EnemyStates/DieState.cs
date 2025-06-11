using UnityEngine;

public class DieState : IState
{
    private readonly EnemyRefrences _refs;

    public DieState(EnemyRefrences refs)
    {
        _refs = refs;
    }

    public void OnEnter()
    {
        // Play death VFX or animation here if desired
        Object.Destroy(_refs.gameObject);
    }

    public void Tick() { }
    public void OnExit() { }
    public Color GizmoColor() => Color.black;
}