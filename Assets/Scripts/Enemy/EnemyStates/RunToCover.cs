using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public class RunToCover : IState
{
    private EnemyRefrences enemy;
    private Transform player;
    private NavMeshAgent agent;
    private float originalSpeed;
    private Vector3 hidePosition;
    public bool IsHidden { get; private set; }

    public RunToCover(EnemyRefrences enemyRefrences)
    {
        enemy = enemyRefrences;
        player = enemy.player;
        agent = enemy.agent;
        IsHidden = false;
    }

    public void OnEnter()
    {
        IsHidden = false;
        if (agent == null || !agent.isOnNavMesh || !agent.isActiveAndEnabled) return;
        
        originalSpeed = agent.speed;
        agent.speed = originalSpeed * 1.5f; // Increase speed when running to cover
        agent.isStopped = false;
        //Debug.Log("RunToCover: Entered state.");
        hidePosition = FindCoverPosition();
        agent.SetDestination(hidePosition);
        enemy.animator?.SetBool("hiding", false);
        enemy.animator?.SetFloat("speed", agent.speed);
    }

    public void Tick()
    {
        if (agent == null || player == null || enemy.transform == null) return;
        if (!agent.isOnNavMesh || !agent.isActiveAndEnabled) return;

        float distToCover = Vector3.Distance(enemy.transform.position, hidePosition);

        if (distToCover > 1f)
        {
            // Still moving to cover
            agent.SetDestination(hidePosition);
            return;
        }

        // Already at cover
        if (CanPlayerSeeEnemy())
        {
            //Debug.Log("Player spotted enemy. Re-hiding...");
            hidePosition = FindCoverPosition();
            agent.SetDestination(hidePosition);
            IsHidden = false;
            enemy.animator?.SetBool("hiding", false);
            enemy.animator?.SetFloat("speed", agent.velocity.magnitude);
        }
        else
        {
            IsHidden = true;
            //Debug.Log("Enemy is hidden and staying put.");
            agent.SetDestination(enemy.transform.position); // stop
            enemy.animator?.SetBool("hiding", true);
            enemy.animator?.SetFloat("speed", 0f);
        }
    }

    public void OnExit()
    {
        // Log cover usage with success indicator
        ActionLogger.Instance?.LogActionWithContext(
            actor: "Enemy",
            actionType: "RunToCover",
            target: "Player",
            isHit: false,
            damage: 0f,
            distance: Vector3.Distance(enemy.transform.position, player.position),
            actorHealthPercent: enemy.GetComponent<Enemy>()?.CurrentHealth / enemy.GetComponent<Enemy>()?.maxHealth ?? -1f,
            targetHealthPercent: PlayerData.playerHealth / 100f,
            actorState: "Cover",
            wasSuccessful: IsHidden
        );
        
        IsHidden = false;
        agent.speed = originalSpeed; 
        //Debug.Log("RunToCover: Exited state.");
        enemy.animator?.SetBool("hiding", false);
        enemy.animator?.SetFloat("speed", 0f);
    }

    public Color GizmoColor() => Color.yellow;

    private Vector3 FindCoverPosition()
    {
        const int    rays     = 16;
        const float  radius   = 10f;
        Vector3      bestPos  = enemy.transform.position;
        float        bestScore= float.MinValue;
        Vector3      origin   = enemy.transform.position;
        Vector3      playerPos= player.position;

        for (int i = 0; i < rays; i++)
        {
            float angle = (i / (float)rays) * Mathf.PI * 2f;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 sample = origin + dir * radius;

            // 1) Snap to nearest NavMesh
            if (!NavMesh.SamplePosition(sample, out var hit, 1f, NavMesh.AllAreas))
                continue;

            Vector3 pos = hit.position;

            // 2) Ensure the agent can actually path to it
            if (!IsReachable(pos)) 
                continue;

            // 3) Check if player LOS is blocked
            Vector3 toPlayer = playerPos - pos;
            bool blocked = Physics.Raycast(
                pos + Vector3.up, 
                toPlayer.normalized, 
                toPlayer.magnitude, 
                LayerMask.GetMask("Default")
            );

            // 4) Score by “blocked” first, then distance from player
            float score = (blocked ? 1f : 0f) * toPlayer.magnitude;
            if (score > bestScore)
            {
                bestScore  = score;
                bestPos    = pos;
            }
        }

        // Fallback: if no blocked spot found, just back away from the player
        if (bestScore <= 0f)
        {
            Vector3 away = (origin - playerPos).normalized * (radius * 0.5f);
            if (NavMesh.SamplePosition(origin + away, out var fb, 1f, NavMesh.AllAreas))
                bestPos = fb.position;
        }

        return bestPos;
    }

    private bool IsReachable(Vector3 target)
    {
        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(target, path))
            return path.status == NavMeshPathStatus.PathComplete;
        return false;
    }

    private bool CanPlayerSeeEnemy()
    {
        Vector3 dirToPlayer = player.position - enemy.transform.position;
        Ray ray = new Ray(enemy.transform.position + Vector3.up, dirToPlayer.normalized);
        return !Physics.Raycast(ray, dirToPlayer.magnitude, LayerMask.GetMask("Default"));
    }
}
