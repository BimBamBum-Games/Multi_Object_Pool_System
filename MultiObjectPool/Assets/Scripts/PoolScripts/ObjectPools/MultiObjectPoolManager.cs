using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MultiObjectPoolManager : MonoBehaviour {
    //Editorde gosterileceginden dolayi new anahtariyla global olarak olusturuldu aksi durumda referans null hatasi alinir.
    public PoolObjectContainerSO poolObjectContainerSCO;
    public SerializableDictionary<PoolObjectWrapper, ObjectPool<PoolObject>> objectPoolDictionary = new();
    //Olusturulan her bir instance icin yasam suresi belirlemek amaciyle tutulan deger tipi alanidir.
    [SerializeField] [Min(0.00005f)] float _spanTimeForInstance = 0.5f;
    //Topluca stacklemek amaciyla kullanilacak olan action delegate alanidir.
    Action _onStack;

    //Rasgele seri sekilde uretim deger tipi alanidir.
    [HideInInspector] public bool canGenerateEndless;

    private void Awake() {
        InitializeLetterPools();
        GenerateByCount();
    }
    private void Update() {
        if (canGenerateEndless) {
            GenarateByTime();
        }
        //GetSensorDetails();
    }

    private void OnDisable() {
        FlushStats();
    }
    /// <summary> 
    /// var pow = poolObjectWrapper; Closure ve Unassigned problemi ortadan kaldirilir.
    /// ObjectPool<PoolObject> objectPool = null; Her ne kadar CreateLetterElement(pow, objectPool) null olsa bile referans
    /// degerli oldugundan ctor tamamlandiktan sonra fererans atanmis olacagindan problem olusturmaz.
    /// <summary> 
    private void InitializeLetterPools() {
        //Object Poollar olusturularak depolanma surecine gecilir.
        foreach (var pool_object_wrapper in poolObjectContainerSCO.items) {
            //Her bir model icin bir Object Pool olusturulur ve initalize edilir.
            //PoolPAckage olustur ve goruntulemek amaciyla listeye gonder.          
            var pow = pool_object_wrapper;
            
            //pow.ResetStats();

            ObjectPool<PoolObject> objectPool = null;
            //Debug.Log("Test Wrapper");
            objectPool = new ObjectPool<PoolObject>(
                () => CreateInstanceElement(pool_object_wrapper, objectPool),
                (poolObject) => { GetInstanceFromPool(pool_object_wrapper, poolObject); },
                (poolObject) => { ReturnInstanceToPool(pool_object_wrapper, poolObject); },
                (poolObject) => { DestroyInstanceWhenOverCapacity(pool_object_wrapper, poolObject); },
                pool_object_wrapper.checkable,
                pool_object_wrapper.min,
                pool_object_wrapper.max
            );

            //Her olusturulan object pool dictionarye eklenerek yedeklenir.
            objectPoolDictionary.Add(pool_object_wrapper, objectPool);
        }
    }

    private PoolObject CreateInstanceElement(PoolObjectWrapper pow, ObjectPool<PoolObject> op) {
        // Belirli bir harfi temsil eden harf modelini oluþtur
        PoolObject letterObject = Instantiate(pow.poolObject);
        letterObject.spanTime = _spanTimeForInstance;
        letterObject.AssignPool(op);
        pow.total++;
        return letterObject;
    }

    private void GetInstanceFromPool(PoolObjectWrapper pow, PoolObject letterObject) {
        // Harf modelini havuzdan alýrken yapýlacak iþlemler
        letterObject.SetSpanTime(_spanTimeForInstance);
        letterObject.transform.position = transform.position;
        letterObject.gameObject.SetActive(true);
        pow.active++;
        GetSensorDetails();
    }

    private void ReturnInstanceToPool(PoolObjectWrapper pow, PoolObject letterObject) {
        // Harf modelini havuza geri gönderirken yapýlacak iþlemler
        letterObject.gameObject.SetActive(false);
        pow.active--;
        GetSensorDetails();
    }

    private void DestroyInstanceWhenOverCapacity(PoolObjectWrapper pow, PoolObject letterObject) {
        // Kapasite fazlasýndaki harf modelini yok etme iþlemleri
        Destroy(letterObject.gameObject);
        pow.total--;
        GetSensorDetails();
    }

    //GC yuku azaltmak amaciyladir.
    PoolObject _generate_by_count;
    private void GenerateByCount() {
        //Stack Alma 1. Yontem Tum GO lar olusturuldugunda hepsi bir anda bir callback ile queuee ye islenir. Pool Count sifirsa _onStack null olur. Nullable isaretlenmelidir.
        foreach (var op in objectPoolDictionary.Keys) {
            //Debug.Log("Test : GenerateByCount I. Loop Calistirildi!");
            for (int i = 0; i < op.min; i++) {
                //Debug.Log("Test : GenerateByCount II. Loop Calistirildi!");

                _generate_by_count = objectPoolDictionary[op].Get();
                _onStack += _generate_by_count.PushBack2Pool;
            }
        }
        _onStack?.Invoke();
        _onStack = null;
    }

    float _generatorTimeMeter;
    [SerializeField] [Min(0.00005f)] float _generationTimeInterval = 0.5f;
    ObjectPool<PoolObject> _pool_tmp;
    PoolObjectWrapper _get_random_pool_from_dictionary;
    private void GenarateByTime() {
        //Belirli zaman araliklarinda GO olusturulur.
        _generatorTimeMeter += Time.deltaTime;
        if (_generatorTimeMeter > _generationTimeInterval) {
            //Eger aranan random indexli pool mevcutsa o pooldan bir PoolObject kopartilir.
            _pool_tmp = GetRandomPoolFromDictionary();
            _pool_tmp?.Get();
            _generatorTimeMeter = 0;
        }
    }

    private ObjectPool<PoolObject> GetRandomPoolFromDictionary() {
        //Rasgele uretilen sayiya karsilik gelen key degeriyle eslesen value bulunur.
        if(objectPoolDictionary.Count == 0) return null;    
        int rnd = Random.Range(0, objectPoolDictionary.Count);
        _get_random_pool_from_dictionary = objectPoolDictionary.Keys.ElementAt(rnd);
        return objectPoolDictionary[_get_random_pool_from_dictionary];
    }

    //Bu Sensor Update islemlerinden sonra event amacli kullanilmaktadir. Repaint basta olmak uzeredir.
    [HideInInspector] public Action onMethodCalled;

    private void GetSensorDetails() {
        //Dict ile foreach cok performansli calismaktadir. Create, Release, Destroy aninda sensorleri gunceller.      
        foreach (KeyValuePair<PoolObjectWrapper, ObjectPool<PoolObject>> op in objectPoolDictionary) {
            op.Key.inactive = op.Key.total - op.Key.active;
        }
        //Repaint metodunu belirli tetiklemeler ile performans saðlamak amacýyla burada cagirir. Repaint update gibi cagirldigindan performansi etkileyebilir.
        onMethodCalled?.Invoke();
    }

    public void FlushStats() {
        //Tum PoolObjectWrapper ScriptibleObje leri OnDisable aninda resetler.
        foreach (var pool_object_wrapper in poolObjectContainerSCO.items) {
            pool_object_wrapper.ResetStats();
        }
    }

    public void GetWithDemand(string poolName) {
        bool check_name = false;
        foreach (KeyValuePair<PoolObjectWrapper, ObjectPool<PoolObject>> op in objectPoolDictionary) {
            //Verilen string ile uyusan pool adi varsa bulup pooldan ornek cekilir. Eger Pool izni verilmisse.
            if(op.Key.poolName == poolName && op.Key.canBePooled == true) {
                op.Value.Get();
                check_name = true;
                Debug.Log("Istenen Pooldan Istenen Pool Objesi Getirildi!");
            }
        }
        if (check_name == false) {
            Debug.LogWarning("Boyle Bir Key Bulunamadi!");
        }
    }
}



#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(MultiObjectPoolManager), true)]
public class MultiObjectPoolManagerEditor : Editor {
    MultiObjectPoolManager _mopm;
    SerializedProperty _canEndless, _sub_srp00, _sub_srpAllowance, _sub_srp01, _sub_srp02, _sub_srp03, _sub_srp04;
    GUIStyle _labelTitleMidCenter;
    SerializedProperty _sps;
    string _poolName = string.Empty;
    private void OnEnable() {

        //MOPM instance referansi alinir. 0. layer
        _mopm = (MultiObjectPoolManager)target;

        //Daha verimli bir yontem olarak repaint metodunu bu editor classinin bagli oldugu monobehaviour ile tetikleterek gerektiginde guncelletilir.
        _mopm.onMethodCalled += RepaintOnDemand;
        
        //Endless olarak rasgele spawn saglamak maksadiyla tutulan deger tipi alani icin serialized property alanidir.
        _canEndless = serializedObject.FindProperty(nameof(MultiObjectPoolManager.canGenerateEndless));

        //Label Title Style konfigurasyonu saglanir.
        _labelTitleMidCenter = new();
        _labelTitleMidCenter.alignment = TextAnchor.MiddleCenter;
        _labelTitleMidCenter.fontStyle = FontStyle.Bold;
        _labelTitleMidCenter.normal.textColor = Color.green;
    }

    private void OnDisable() {
        //Mem serbest birakilir.
        _mopm.onMethodCalled -= RepaintOnDemand;
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();
        DrawDefaultInspector();

        EditorGUILayout.PropertyField(_canEndless);

        if(Application.isPlaying) {
            //Istenen obje adiyla aranip buttonla test etmek amaciyla cagrilir. Playmodda calisir.
            EditorGUILayout.Space(20);
            GUILayout.Label("Play Modda Calisir!");
            EditorGUIUtility.labelWidth = 100;
            _poolName = EditorGUILayout.TextField("Object Name: ", _poolName).ToUpper();
            if (GUILayout.Button("Get Instance From A Specific Pool")) {
                Debug.LogWarning("Pool Adi : " + _poolName);
                _mopm.GetWithDemand(_poolName);
            }
        }

        EditorGUILayout.Space(20);
        EditorGUIUtility.labelWidth = 100;
        if (GUILayout.Button("Repaint Inspector On Demand")) {
            Debug.LogWarning("Repaint Metodu Cagrildi Ekran Yenilendi!");
            RepaintOnDemand();
        }

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Specs For Individiual Object Pools In", _labelTitleMidCenter);
        EditorGUILayout.Space(10);
        _sps = serializedObject.FindProperty(nameof(MultiObjectPoolManager.poolObjectContainerSCO));

        //Eger container ise bu kosula girer ve surecleri isletir.
        SerializedObject so = new(_sps.objectReferenceValue);
                
        SerializedProperty sp = so.FindProperty(nameof(PoolObjectContainerSO.items));
        //Debug.LogWarning(sp.propertyType + " type test");

        for (int i = 0; i < sp.arraySize; i++) {
            //Aranan serialized property bir liste (unity generic datatype sayar) index ile cagrilirlar.             
            _sub_srp00 = sp.GetArrayElementAtIndex(i);
            //Debug.LogWarning("Type test " + _sub_srp00.propertyType);
            SerializedObject scriptible_object_reference = new(_sub_srp00.objectReferenceValue);
            scriptible_object_reference.Update();
            _sub_srpAllowance = scriptible_object_reference.FindProperty(nameof(PoolObjectWrapper.canBePooled));
            _sub_srp01 = scriptible_object_reference.FindProperty(nameof(PoolObjectWrapper.poolName));
            _sub_srp02 = scriptible_object_reference.FindProperty(nameof(PoolObjectWrapper.total));
            _sub_srp03 = scriptible_object_reference.FindProperty(nameof(PoolObjectWrapper.inactive));
            _sub_srp04 = scriptible_object_reference.FindProperty(nameof(PoolObjectWrapper.active));
            BalanceLabels();
            //Her bir elde edilmis object referansli alanlar uzerinde yapilan degisimleri saglayabilmek ve kaydedebilmek icin update ve apply kullanilmasi gerekmektedir.
            scriptible_object_reference.ApplyModifiedProperties();
        }

        serializedObject.ApplyModifiedProperties();
    }

    public void BalanceLabels() {
        //Genislikleri belirle label ve field icin ayri ayri olarak belirlenir.
        EditorGUIUtility.labelWidth = 55;
        EditorGUIUtility.fieldWidth = 25;
        EditorGUILayout.BeginHorizontal();      
        EditorGUILayout.PropertyField(_sub_srpAllowance);
        //Buradan sonraki grubu tiklanamaz olarak isaretler.
        EditorGUI.BeginDisabledGroup(true);
        GUIContent labelCnt = new("Name");
        EditorGUILayout.PropertyField(_sub_srp01);
        EditorGUILayout.PropertyField(_sub_srp02);
        EditorGUILayout.PropertyField(_sub_srp03);
        EditorGUILayout.PropertyField(_sub_srp04);
        //Disable grubun sonunda enable hale getir.
        EditorGUI.EndDisabledGroup();
        //Yatay bir duzeni kapat
        EditorGUILayout.EndHorizontal();
    }

    private void RepaintOnDemand() {
        //Action olarak kullanilacaktir.
        Repaint();
        //Debug.Log("Test : Repaint Istek Durumunda Cagrildi!");
    }

    private string GetPropertyValue(SerializedProperty property) {
        //Hangi tip oldugunu otomatik bulmak ve hangisi neydi diye ugrasmamak icin kod parcasidir.
        switch (property.propertyType) {
            case SerializedPropertyType.Integer:
                return property.intValue.ToString();
            case SerializedPropertyType.Float:
                return property.floatValue.ToString();
            case SerializedPropertyType.Boolean:
                return property.boolValue.ToString();
            case SerializedPropertyType.String:
                return property.stringValue;
            default:
                return "Unsupported Type";
        }
    }
}
#endif