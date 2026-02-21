using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class StateMachine : MonoBehaviour
{
    private IState currentState;

    private Dictionary<Type, List<Transition>> transitions = new Dictionary<Type, List<Transition>>();
    private List<Transition> currentTransitions = new List<Transition>();
    private List<Transition> anyTransition = new List<Transition>();
    private static List<Transition> emptyTransitions = new List<Transition>(0);

    private class Transition
    {
        public Func<bool> condition { get; }
        public IState to { get; }

        public Transition(Func<bool> condition, IState to)
        {
            this.condition = condition;
            this.to = to;
        }
    }

    public void Tick()
    {
        var transition = GetTransitions();
        if (transition != null)
        {
            Debug.Log($"[FSM] Transitioning to: {transition.to.GetType().Name}");
            SetState(transition.to);
        }

        currentState?.Tick();
    }

    public void SetState(IState state)
    {
        if(state == currentState) 
        {
            Debug.Log($"[FSM] Attempted SetState to {state.GetType().Name}, but it's already current.");
            return; 
        }
        
        Debug.Log($"[FSM] Switching state to: {state.GetType().Name}");

        currentState?.OnExit();

        currentState = state;

        transitions.TryGetValue(currentState.GetType(), out currentTransitions);
        if (currentTransitions == null)
        {
            currentTransitions = emptyTransitions;
        }

        currentState.OnEnter();
    }

    public void AddTransition(IState from, IState to, Func<bool> condition)
    {
        if (transitions.TryGetValue(from.GetType(), out var targetTransitions) == false)
        {
            targetTransitions = new List<Transition>();
            transitions[from.GetType()] = targetTransitions;
        }

        targetTransitions.Add(new Transition(condition, to));
    }

    public void AddAnyTransition(IState to, Func<bool> condition)
    {
        anyTransition.Add(new Transition(condition, to));
    }

    private Transition GetTransitions()
    {
        foreach (Transition transition in anyTransition)
        {
            if (transition.condition())
            {
                return transition;
            }
        }

        foreach (Transition transition in currentTransitions)
        {
            if (transition.condition())
            {
                return transition;
            }
        }
        return null;
    }

    public Color GizmoColor()
    {
        if (currentState != null)
        {
            return currentState.GizmoColor();
        }

        return Color.white;
    }
}