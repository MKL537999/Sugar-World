using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : Component
{
    private readonly T prefab;
    private readonly Transform parent;
    private readonly Queue<T> pool = new Queue<T>();
    private readonly int prewarmCount;

    public ObjectPool(T prefab, int prewarmCount = 10, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.prewarmCount = prewarmCount;

        for (int i = 0; i < prewarmCount; i++)
        {
            var obj = CreateNew();
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    private T CreateNew()
    {
        var obj = Object.Instantiate(prefab, parent);
        obj.name = prefab.name;
        return obj;
    }

    public T Get()
    {
        T obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            obj = CreateNew();
        }
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void Release(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }

    public int ActiveCount { get; set; }
}
