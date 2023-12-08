using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "new PoolObjectWrapper", menuName = "Multi Object Pool/Pool Object Wrapper")]
public class PoolObjectWrapper : ScriptableObject {
    public PoolObject poolObject;
    [SerializeField] bool _showLogs = true;
    public string poolName = string.Empty;
    [HideInInspector] public int min = 1;
    [HideInInspector] public int max = 10;
    public bool checkable;

    public int total, active, inactive;
    public bool canBePooled = true;

    //Awake ile de cagrilabilir ancak bazen ekleme ve cikarmalarda farkinda olmadan yeni obje olusturulmazsa null object referenc hatasi verebilir. Bu problemin anlasilmasini guclestirebilir.
    //Awake ancak awake oldugunda cagrilir. Ancak bu sekilde ctor esnasinda cagrilacagindan daha temiz yontemdir.
    public static List<PoolObjectWrapper> PoolObjectWrappers = new();

    public void ResetStats() {
        //Play2Edit ve Edit2Play durumlarinda resetlenir.
        total = 0;
        active = 0;
        inactive = 0;
    }

    private void OnEnable() {
        //SO olusturuldugu anda zaten bu cagrilmis olacak yani Playmode oncesi olusturulurdugunda Editmode icinde cagrilir.
        if(_showLogs) Debug.LogWarning("Test : PoolObjectWrapper SObject: " + name + " Pool Adi: " + poolName + " Listyeye Eklendi!");
        PoolObjectWrappers.Add(this);
    }

    private void OnDestroy() {
        //Tus ile delete edildiginde OnDestroy ve OnDisable cagrilmiyor.
        //Debug.Log("Ben silindim!");
    }

    ~PoolObjectWrapper() {
        if(_showLogs) Debug.Log("Test : Ben silindim!"); 
        PoolObjectWrappers.Remove(this);
    }

    //public static PoolObjectWrapper GetRandomPoolObjectWrapper() {
    //    //Statik listeden random olarak kendi tipinde obje ister.
    //    if (PoolObjectWrappers.Count == 0) {
    //        //Sifir elemanli listeden sifirnci index alinamaz bu nedenle null doner.
    //        return null;
    //    }
    //    int rnd = Random.Range(0, PoolObjectWrappers.Count);
    //    return PoolObjectWrappers[rnd];
    //}
}

#if UNITY_EDITOR
//Bu attribute ile coklu editleme saglanabilmektedir.
[CanEditMultipleObjects]
[CustomEditor(typeof(PoolObjectWrapper), true)]
public class PoolObjectWrapperEditor : Editor {
    PoolObjectWrapper _poolObjectWrapper;
    SerializedProperty _min, _max;
    private void OnEnable() {
        //Monobehaviour referansinin kopyasi tutulur.
        _poolObjectWrapper = (PoolObjectWrapper)target;
        _min = serializedObject.FindProperty(nameof(PoolObjectWrapper.min));
        _max = serializedObject.FindProperty(nameof(PoolObjectWrapper.max));
    }

    public override void OnInspectorGUI() {
        // Serilestirmeden kaynakli degerlerde degisim oldugunda otomatik update edilecektir. SerializedObject update edilir.

        serializedObject.Update();
        DrawDefaultInspector();

        EditorGUILayout.LabelField("Minimum Stock and Maximum Allowed Stock");

        //Eger min maxtan buyukse maxi mine esitler. Her iki durumda da max ve min alanlarindaki degisime gore ayni ayarlamalar yapilir.
        EditorGUI.BeginChangeCheck();
        _min.intValue = EditorGUILayout.IntField("Min", _min.intValue);
        if (EditorGUI.EndChangeCheck()) {
            if(_min.intValue < 0) _min.intValue = 0;
            if (_min.intValue > _max.intValue) {
                _max.intValue = _min.intValue;
            }
        }

        EditorGUI.BeginChangeCheck();
        _max.intValue = EditorGUILayout.IntField("Max", _max.intValue);
        if (EditorGUI.EndChangeCheck()) {
            if (_max.intValue < 0) _max.intValue = 0;
            if (_max.intValue < _min.intValue) {
                _min.intValue = _max.intValue;
            }
        }

        GUILayout.Label("Test Maksatli Resetleme Buttonu");
        if (GUILayout.Button("Reset Values")) {
            //Eger gerekirse manual olarak resetlenebilir.
            _poolObjectWrapper.ResetStats();
        }

        //Her bir begin check alani ayri ayri olarak alanlari degerlendirmeye alir. Bu sayede olasi deger cakismalari engellenir.
        serializedObject.ApplyModifiedProperties();
    }
}
#endif