using System;
using UnityEngine;

/// <summary>
/// M5 event boundary. Gameplay systems consume shot lifecycle facts here instead of
/// polling Rigidbody state or calling one another directly.
/// </summary>
public sealed class M5ShotEventContract : MonoBehaviour
{
    public event Action<int> ShotStarted;
    public event Action<int> PhysicsSettled;

    [SerializeField] private M5ShotLifecycle lifecycle;

    public M5ShotLifecycle Lifecycle => lifecycle;

    private void Awake()
    {
        if (lifecycle == null)
            lifecycle = GetComponent<M5ShotLifecycle>();
        if (lifecycle == null)
            lifecycle = FindObjectOfType<M5ShotLifecycle>();
    }

    private void OnEnable()
    {
        InitializeBindings();
    }

    public void InitializeBindings()
    {
        if (lifecycle == null)
            lifecycle = GetComponent<M5ShotLifecycle>();
        if (lifecycle == null)
            lifecycle = FindObjectOfType<M5ShotLifecycle>();
        if (lifecycle == null)
            return;

        lifecycle.ShotStarted -= HandleShotStarted;
        lifecycle.ShotSettled -= HandlePhysicsSettled;
        lifecycle.ShotStarted += HandleShotStarted;
        lifecycle.ShotSettled += HandlePhysicsSettled;
    }

    private void OnDisable()
    {
        if (lifecycle == null)
            return;
        lifecycle.ShotStarted -= HandleShotStarted;
        lifecycle.ShotSettled -= HandlePhysicsSettled;
    }

    private void HandleShotStarted()
    {
        ShotStarted?.Invoke(lifecycle.ShotSequence);
    }

    private void HandlePhysicsSettled()
    {
        PhysicsSettled?.Invoke(lifecycle.ShotSequence);
    }
}
