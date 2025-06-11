using UnityEngine;

public class IdleState : IState
{
    private readonly EnemyRefrences _refs;
    public IdleState(EnemyRefrences refs) => _refs = refs;

    public void OnEnter() { /* no animation trigger by default */ }
    public void Tick()  { /* do nothing while idle */ }
    public void OnExit() { /* cleanup if needed */ }
    public Color GizmoColor() => Color.gray;
}