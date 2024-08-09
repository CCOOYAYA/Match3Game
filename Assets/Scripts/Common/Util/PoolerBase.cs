using System;
using UnityEngine;
using UnityEngine.Pool;


public abstract class PoolerBase<T> : MonoBehaviour where T : MonoBehaviour
{
    private T _prefab;
    private ObjectPool<T> _pool;

    private ObjectPool<T> Pool
    {
        get
        {
            if (_pool == null) throw new InvalidOperationException("You need to call InitPool before using it.");
            return _pool;
        }
        set => _pool = value;
    }

    protected void InitializePool(T prefab, int initial, int max, bool collectionChecks = true)
    {
        _prefab = prefab;
        Pool = new ObjectPool<T>(
            CreateSetup,
            OnGetSetup,
            OnReleaseSetup,
            OnDestroySetup,
            collectionChecks,
            initial,
            max);
    }

    #region Overrides
    protected virtual T CreateSetup() => Instantiate(_prefab);
    protected virtual void OnGetSetup(T obj) => obj.gameObject.SetActive(true);
    protected virtual void OnReleaseSetup(T obj) => obj.gameObject.SetActive(false);
    protected virtual void OnDestroySetup(T obj) => Destroy(obj);
    #endregion

    #region Getters
    public T Get() => Pool.Get();
    public void Release(T obj) => Pool.Release(obj);
    #endregion
}