using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolObject : MonoBehaviour
{
    ObjectPool<PoolObject> _pool;
    public float spanTime;
    private float _enemyBlockTimeMeter = 0;
    private bool _canDie = false;

    protected virtual void Update() {
        if (_enemyBlockTimeMeter < spanTime) {
            _enemyBlockTimeMeter += Time.deltaTime;
        }
        else {
            _canDie = true;
        }

        if (_canDie) {
            //Debug.LogAssertion("I will die immediately");
            PushBack2Pool();
        }
    }
    public void AssignPool(ObjectPool<PoolObject> pool) {
        //Ait oldugu havuzun referansini bildir.
        _pool = pool;
        Dl.Wr("Test : GO Havuza atandi.");
    }

    public void SetSpanTime(float spanTime) {
        _canDie = false;
        _enemyBlockTimeMeter = 0;
        this.spanTime = spanTime;
    }

    public void PushBack2Pool() {
        //Nullable cunku bir prefab degilse ve sahnedeyse bu kisim Pool referansi atanamadigindan hata verir.
        Dl.Wr("Test : Bu Instance Havuza Geri Gonderildi!");
        _pool?.Release(this);
    }
}
