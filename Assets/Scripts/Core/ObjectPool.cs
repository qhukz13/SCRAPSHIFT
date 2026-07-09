using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMaintenance.Core
{
    public class ObjectPool<T> where T : MonoBehaviour
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _pool = new Queue<T>();

        public ObjectPool(T prefab, int initialSize, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateObject();
                obj.gameObject.SetActive(false);
                _pool.Enqueue(obj);
            }
        }

        private T CreateObject()
        {
            var obj = UnityEngine.Object.Instantiate(_prefab, _parent);
            return obj;
        }

        public T Get()
        {
            if (_pool.Count > 0)
            {
                var obj = _pool.Dequeue();
                obj.gameObject.SetActive(true);
                return obj;
            }

            var newObj = CreateObject();
            newObj.gameObject.SetActive(true);
            return newObj;
        }

        public void ReturnToPool(T obj)
        {
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
    }
}
