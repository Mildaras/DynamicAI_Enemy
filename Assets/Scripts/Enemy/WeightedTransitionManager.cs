using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

public class WeightedTransition
{
    public IState From { get; }
    public IState To   { get; }
    private readonly Func<float> _weightEvaluator;

    public WeightedTransition(IState from, IState to, Func<float> weightEvaluator)
    {
        From             = from;
        To               = to;
        _weightEvaluator = weightEvaluator;
    }

    public float GetWeight() => Mathf.Max(0f, _weightEvaluator());
}

[RequireComponent(typeof(StateMachine))]
public class WeightedTransitionManager : MonoBehaviour
{
    [Tooltip("Seconds between re-evaluations of weighted rules")]
    [SerializeField] private float evaluationInterval = 1f;

    private StateMachine _fsm;
    private FieldInfo _currentStateField;
    private IState _defaultTarget;
    private float _timer;
    private readonly Dictionary<IState, List<WeightedTransition>> _rulesByState = new();

    void Awake()
    {
        _fsm = GetComponent<StateMachine>()
            ?? throw new InvalidOperationException("WeightedTransitionManager requires a StateMachine component.");

        _currentStateField = typeof(StateMachine)
            .GetField("currentState", BindingFlags.Instance | BindingFlags.NonPublic);
        if (_currentStateField == null)
            Debug.LogError("[Weighted] Could not find private 'currentState' field on StateMachine");
    }

    void Update()
    {
        // Throttle weight evaluations
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = evaluationInterval;

        TryWeightedTransition();
    }

    private IState GetCurrentState()
        => (IState)_currentStateField.GetValue(_fsm);

    private void TryWeightedTransition()
    {
        var current = GetCurrentState();
        if (current == null) return;

        // Only proceed if there are rules for this state
        if (!_rulesByState.TryGetValue(current, out var options) || options.Count == 0)
            return;

        float total = 0f;
        var eligible = new List<WeightedTransition>(options.Count);
        var cumulative = new List<float>(options.Count);

        // Evaluate each rule's weight
        foreach (var rule in options)
        {
            float w = rule.GetWeight();
            if (w <= 0f) continue;
            total += w;
            eligible.Add(rule);
            cumulative.Add(total);
        }

        if (total <= 0f) return;

        // Sample a transition based on weighted random
        float roll = UnityEngine.Random.value * total;
        int idx = cumulative.BinarySearch(roll);
        if (idx < 0) idx = ~idx;
        idx = Mathf.Clamp(idx, 0, eligible.Count - 1);

        _fsm.SetState(eligible[idx].To);
    }

    /// <summary>
    /// Registers a weighted transition rule. Rules are grouped by their 'From' state.
    /// </summary>
    public void AddRule(WeightedTransition rule)
    {
        if (!_rulesByState.TryGetValue(rule.From, out var list))
        {
            list = new List<WeightedTransition>();
            _rulesByState[rule.From] = list;
        }
        list.Add(rule);
    }

    public void AddRule(IState from, Func<float> weightEvaluator)
    {
        if (_defaultTarget == null)
            Debug.LogError("WeightedTransitionManager: default target is null!  Call SetDefaultTarget(...) first.");
        else
            AddRule(new WeightedTransition(from, _defaultTarget, weightEvaluator));
    }

    public void SetDefaultTarget(IState defaultTo)
    {
        _defaultTarget = defaultTo;
    }
}
