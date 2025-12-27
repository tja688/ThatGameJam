using System.Collections.Generic;
using UnityEngine;

namespace ThatGameJam.Features.FallingRockFromTrashCan.Utilities
{
    public class SimpleGameObjectPool
    {
        private readonly Queue<GameObject> _pool = new Queue<GameObject>();
        private readonly GameObject _prefab;
        private readonly Transform _parent;

        public SimpleGameObjectPool(GameObject prefab, int preload, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;

            if (_prefab == null)
            {
                return;
            }

            for (var i = 0; i < Mathf.Max(0, preload); i++)
            {
                var instance = Object.Instantiate(_prefab, _parent);
                instance.SetActive(false);
                _pool.Enqueue(instance);
            }
        }

        public GameObject Get()
        {
            if (_prefab == null)
            {
                return null;
            }

            if (_pool.Count > 0)
            {
                var instance = _pool.Dequeue();
                if (instance != null)
                {
                    instance.SetActive(true);
                }

                return instance;
            }

            return Object.Instantiate(_prefab, _parent);
        }

        public void Return(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.SetActive(false);
            _pool.Enqueue(instance);
        }

        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var instance = _pool.Dequeue();
                if (instance != null)
                {
                    Object.Destroy(instance);
                }
            }
        }
    }
}
