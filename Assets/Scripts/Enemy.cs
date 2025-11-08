using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float arrivalDistance = 0.05f;
    public Vector3 targetPosition = Vector3.zero;

    [Header("Events")]
    public UnityEvent onArrived;

    Action<Enemy> _arrivedCallback;
    bool _arrived;

    public void Initialize(Vector3 target, Action<Enemy> onArrivedCallback = null)
    {
        targetPosition = target;
        _arrivedCallback = onArrivedCallback;
    }

    void Update()
    {
        if (_arrived) return;

        Vector3 current = transform.position;
        float step = moveSpeed * Time.deltaTime;
        Vector3 next = Vector3.MoveTowards(current, targetPosition, step);
        transform.position = next;

        if ((next - targetPosition).sqrMagnitude <= (arrivalDistance * arrivalDistance))
        {
            HandleArrived();
        }
    }

    void HandleArrived()
    {
        if(_arrived) return;
        _arrived = true;

        try
        {
            onArrived?.Invoke();
        }
        catch (Exception) { /* ignore user event exceptions */ }

        try
        {
            if(_arrivedCallback != null)
            {
                _arrivedCallback.Invoke(this);
            }
            else
            {
                SpawnerManager.Instance?.OnEnemyArrived(this);
            }
        }
        catch (Exception) { /* ignore callbacks if none registered */ }
    }
}


