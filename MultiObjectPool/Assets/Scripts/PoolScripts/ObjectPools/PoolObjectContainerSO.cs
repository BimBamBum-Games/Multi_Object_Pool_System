using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "new PoolObjectContainer", menuName = "Multi Object Pool/Pool Object Container")]
public class PoolObjectContainerSO : AbstractListSO<PoolObjectWrapper> {
    public int index = 0;
    public string containterName = "Default Container";

    //Eger bir degisim kontrolcusu olarak kullanilacaksa Transform.hasChanged gibi manuel olarak harici classta tekrar setlenmesi gerekmektedir.
    public bool HasChanged { get; set; }

    public void OnInspectorValueChanged() {
        //Editor ile de degisiklikler takibe alinabilir.
        HasChanged = true;
        Debug.LogWarning("Liste Event Ile Degistirildi!" + " " + HasChanged);
    }

    //CanBePooled kurali isletilmesi icin gerekli olan liste referansidir.
    [SerializeField] List<PoolObjectWrapper> _itemsCanBePooled = new();
    public List<PoolObjectWrapper> GetAvailablePoolsFromDictionary() {
        //Random Poollari eger izin verilmisse cekebilmek amaciyla kullanilacak olan metoddur.
        _itemsCanBePooled.Clear();
        foreach(var pow in items) {
            if(pow.canBePooled == true) {
                _itemsCanBePooled.Add(pow);
            }
        }
        return _itemsCanBePooled;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PoolObjectContainerSO))]
public class PoolObjectContainerSOEditor : Editor {
    private PoolObjectContainerSO _poolObjectContainerSO;
    public void OnEnable() {
        _poolObjectContainerSO = (PoolObjectContainerSO)target;
    }

    public override void OnInspectorGUI() {
        //Degisim oldugunda tetiklenir.
        serializedObject.Update();

        GUILayout.Space(20);
        GUILayout.Label("Test Maksatli Random PoolObjectWrapper Buttonu");
        if (GUILayout.Button("Get Random Pool")) {
            //Eger gerekirse random olarak statik listeden cekilebilir.
            List<PoolObjectWrapper> pool_object_wrapper = _poolObjectContainerSO.GetAvailablePoolsFromDictionary();
            for(int i = 0; i < pool_object_wrapper.Count; i++) {
                Debug.Log("Random SObject Name: " + pool_object_wrapper[i].name + " Random Pool Name : " + pool_object_wrapper[i].poolName);
            }        
        }

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck()) {
            //OnInspectorGUI uzerinde yapilan degisiklikte bildirilir.
            _poolObjectContainerSO.OnInspectorValueChanged();
        }
        EditorGUI.EndChangeCheck();

        if (GUILayout.Button("Reset")) {
            //Test amacli resetleyici button.
            _poolObjectContainerSO.HasChanged = false;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif

public class AbstractListSO<T> : ScriptableObject {
    //Event tabanli Sciptible Object liste sinifidir. Enable da setlenebilir.
    public Action<T> OnAdd, OnRemoved, OnChanged; 
    public Action OnClear;
    public List<T> items = new();
    public int Counts { 
        get { 
            return items.Count; 
        } 
        private set { }
    }

    public bool HasAnyItem { 
        //Herhangi bir item ekliyse true degilse false dondurur.
        get { 
            if(items.Count > 0) return true;
            else return false;
        }
        private set { }
    }

    public T this[int index] {
        //Ilgili indexte deger degistirildiginde olay tetiklenir.
        get {
            return items[index];
        }
        set {
            items[index] = value;
            OnChanged?.Invoke(value);
        }
    }

    public void Add(T item) {
        //Eger item listede varsa eklemeden cikilir yoksa eklenir.
        if(items.Contains(item)) {
            return;
        }
        items.Add(item);
        OnAdd?.Invoke(item);
    }
    public void Remove(T item) {
        //Ilgili item yoksa mecburen cikar varsa siler ve olay tetikler.
        if (items.Contains(item)) { 
            return;
        }
        items.Remove(item);
        OnRemoved?.Invoke(item);
    }
    public void Clear() {
        //Eger herhangi bir item listede mevcut degilse silmeden metoddan cikar.
        if (!HasAnyItem) {
            return;
        }
        items.Clear();
        OnClear?.Invoke();
    }
}



