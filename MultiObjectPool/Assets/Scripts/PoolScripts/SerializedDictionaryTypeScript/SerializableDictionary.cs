using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.Pool;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class SerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver {
    [SerializeField]
    private List<TKey> keys = new List<TKey>();

    [SerializeField]
    private List<TValue> values = new List<TValue>();

    private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

    public bool hasChanged = false;

    //public TValue this[TKey key] {

    //    get => dictionary[key];
    //    set => dictionary[key] = value;
    //}

    public TValue this[TKey key] {
        //Interface IDictionary Indexer kullanarak verilere erisim kolaylastirilir. Genelde Dictionaryler ile uyumlu ve sikca kullanilir.
        get {
            hasChanged = false;
            return dictionary[key]; 
        }
        set {
            if (dictionary.ContainsKey(key) && !dictionary[key].Equals(value)) {
                hasChanged = true;
                Debug.Log($"Deðiþiklik algýlandý: {key} deðeri {dictionary[key]} -> {value}");
            }

            dictionary[key] = value;
        }
    }

    //public TValue this[TKey key] {
    //    //Interface IDictionary Indexer kullanarak verilere erisim kolaylastirilir. Genelde Dictionaryler ile uyumlu ve sikca kullanilir.
    //    get => dictionary[key];
    //    set {
    //        if (dictionary.ContainsKey(key)) {
    //            if (!EqualityComparer<TValue>.Default.Equals(dictionary[key], value)) {
    //                Debug.Log($"Value deðiþikliði algýlandý: {key} deðeri {dictionary[key]} -> {value}");
    //            }

    //            if (!EqualityComparer<TKey>.Default.Equals(dictionary.ElementAtOrDefault(key), key)) {
    //                Debug.Log($"Key deðiþikliði algýlandý: {dictionary[key]} -> {key}");
    //            }
    //        }

    //        dictionary[key] = value;
    //    }
    //}

    public ICollection<TKey> Keys {
        //Interface ICollection Key Getter metodudur.
        get {
            return dictionary.Keys;
        }
    }

    public ICollection<TValue> Values { 
        //Interface ICollection Value Getter metodudur.
        get { 
            return dictionary.Values; 
        } 
    }

    public int Count {
        //Interface ICollection Count getter metodudur.
        get {
            return dictionary.Count;
        }
    }

    public bool IsReadOnly {
        //Interface ICollection ReadOnly Getter metodudur.
        get { 
            return false; 
        }
    }

    public void OnBeforeSerialize() {
        //Interface Unity ISerializationCallbackReceiver Serilestirmeden once yapialcak blocktur.
        keys.Clear();
        values.Clear();

        foreach (var pair in dictionary) {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    public void OnAfterDeserialize() {
        //Interface Unity ISerializationCallbackReceiver erilestirmeden sonra yapilacak olan bloktur.
        dictionary.Clear();

        for (int i = 0; i < keys.Count; i++) {
            dictionary[keys[i]] = values[i];
        }
    }

    public Dictionary<TKey, TValue> ToDictionary() {
        //Bu classin serilestirilen dictionary data yapisi referansi dondurulur.
        return dictionary;
    }

    public void Add(TKey key, TValue value) {
        //Interface IDictionary Standart Dictionary yapisina uygun key testi yapilir. Eger zaten mevcut ise hata mesaji dondurulur.
        if (!dictionary.ContainsKey(key)) {
            dictionary.Add(key, value);
        }
        else {
            Debug.LogWarning($"Key '{key}' already exists in the dictionary. Value will not be added.");
        }
    }

    public bool Remove(TKey key) {
        //Interface IDictionary Belirtilen key varsa silinir.
        return dictionary.Remove(key);
    }

    public bool TryGetValue(TKey key, out TValue value) {
        //Interface IDictionary Eger belirtilen key varsa value out degerini doner bool true isaretlenir. Yoksa bool false degerini donecektir. If yapisinda oldukca kullanislidir.
        return dictionary.TryGetValue(key, out value);
    }

    public void Add(string v, KeyValuePair<TKey, TValue> item) {
        //Interface ICollection Dictionarye key value ikilisi eklenmesine olanak tanir.
        Add(item.Key, item.Value);
    }

    public void Clear() {
        //Interface ICollection Dictionary sifirlanir ancak silinmez bu nedenle referans devam eder.
        dictionary.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) {
        //Interface ICollection Bir Key Value ikilisi Dictionary icersinde mevcutluk durumu kontrol edilir.
        return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Contains(item);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
        //Interface ICollection
        ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) {
        //Bir Key Value ikilisi Dictionary icersinden silinir ve duruma gore bool doner.
        return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Remove(item);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
        //Interface IEnumerable implicit implementasyonudur.
        return dictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        //Interface ICollection explicit implementasyonudur.
        return GetEnumerator();
    }

    public bool ContainsKey(TKey key) {
        //IDictionary interface Verilen Key mevcutluguna iliskin bool sonucu dondurulur.
        return dictionary.ContainsKey(key);
    }

    public void Add(KeyValuePair<TKey, TValue> item) {
        ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Add(item);
    }
}