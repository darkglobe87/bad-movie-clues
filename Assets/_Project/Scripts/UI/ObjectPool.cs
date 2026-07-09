using System.Collections.Generic;
using UnityEngine;

namespace BadMovieClues.UI
{
    /// <summary>
    /// A simple, lightweight generic object pool for procedurally generated UI elements.
    /// Helps eliminate garbage collector spikes when transitioning levels or screens.
    /// </summary>
    public class UIObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly List<T> _pool = new List<T>();
        private readonly List<T> _active = new List<T>();

        public IReadOnlyList<T> Active => _active;

        public UIObjectPool(Transform parent, T prefab = null)
        {
            _parent = parent;
            _prefab = prefab;
        }

        /// <summary>Gets or instantiates an element in the pool, parenting it and activating it.</summary>
        public T Get(System.Func<T> factoryMethod = null)
        {
            T element = null;

            if (_pool.Count > 0)
            {
                var lastIndex = _pool.Count - 1;
                element = _pool[lastIndex];
                _pool.RemoveAt(lastIndex);
            }
            else
            {
                if (factoryMethod != null)
                {
                    element = factoryMethod();
                }
                else if (_prefab != null)
                {
                    element = Object.Instantiate(_prefab, _parent, false);
                }
                else
                {
                    Debug.LogError("[UIObjectPool] Attempted to get item but both prefab and factory method are null.");
                    return null;
                }
            }

            element.transform.SetParent(_parent, false);
            element.gameObject.SetActive(true);
            _active.Add(element);
            return element;
        }

        /// <summary>Deactivates all active elements and moves them back to the pool.</summary>
        public void Clear()
        {
            for (var i = 0; i < _active.Count; i++)
            {
                var element = _active[i];
                element.gameObject.SetActive(false);
                _pool.Add(element);
            }
            _active.Clear();
        }
    }
}
