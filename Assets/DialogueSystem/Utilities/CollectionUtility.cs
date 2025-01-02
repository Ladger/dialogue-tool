using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class CollectionUtility
{
    public static void AddItem<K,V>(this SerializableDictionary<K, List<V>> serializableDictionry, K key, V value)
    {
        if (serializableDictionry.ContainsKey(key))
        {
            serializableDictionry[key].Add(value);
            return;
        }

        serializableDictionry.Add(key, new List<V>() { value });
    }
}
