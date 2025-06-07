using UnityEngine;
using System.Collections.Generic;
using DanceInputActions;

public class DanceArrowPool : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string direction;
        public GameObject prefab;
        public int size;
    }

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            if (pool.prefab == null)
            {
                Debug.LogError($"Prefab for direction {pool.direction} is null in DanceArrowPool!", this);
                continue;
            }
            if (!pool.prefab.GetComponent<RectTransform>())
            {
                Debug.LogError($"Prefab for direction {pool.direction} is missing RectTransform! Ensure it’s a UI element.", pool.prefab);
                continue;
            }
            if (!pool.prefab.GetComponent<DanceArrow>())
            {
                Debug.LogError($"Prefab for direction {pool.direction} is missing DanceArrow component!", pool.prefab);
                continue;
            }

            Queue<GameObject> objectPool = new Queue<GameObject>();
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab, transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }
            poolDictionary.Add(pool.direction, objectPool);
        }
    }

    public GameObject GetArrow(string direction)
    {
        if (!poolDictionary.ContainsKey(direction))
        {
            Debug.LogWarning($"Pool with direction {direction} doesn't exist.");
            return null;
        }

        if (poolDictionary[direction].Count == 0)
        {
            ExpandPool(direction);
        }

        GameObject arrow = poolDictionary[direction].Dequeue();
        arrow.SetActive(true);
        DanceArrow danceArrow = arrow.GetComponent<DanceArrow>();
        if (danceArrow != null)
        {
            DanceInput.Instance?.RegisterArrow(danceArrow); // Register with DanceInput
        }
        return arrow;
    }

    public void ReturnArrow(DanceArrow arrow)
    {
        if (arrow == null) return;
        string direction = arrow.direction;
        arrow.gameObject.SetActive(false);
        poolDictionary[direction].Enqueue(arrow.gameObject);
        if (DanceInput.Instance != null)
        {
            DanceInput.Instance.UnregisterArrow(arrow);
        }
    }

    private void ExpandPool(string direction)
    {
        Pool targetPool = pools.Find(p => p.direction == direction);
        if (targetPool == null || targetPool.prefab == null) return;
        GameObject obj = Instantiate(targetPool.prefab, transform);
        obj.SetActive(false);
        poolDictionary[direction].Enqueue(obj);
    }
}
