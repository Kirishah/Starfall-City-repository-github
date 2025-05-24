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

    void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
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
        return arrow;
    }

    public void ReturnArrow(DanceArrow arrow)
    {
        string direction = arrow.direction;
        arrow.gameObject.SetActive(false);
        poolDictionary[direction].Enqueue(arrow.gameObject);
    }

    private void ExpandPool(string direction)
    {
        Pool targetPool = pools.Find(p => p.direction == direction);
        GameObject obj = Instantiate(targetPool.prefab);
        obj.SetActive(false);
        poolDictionary[direction].Enqueue(obj);
    }
}
