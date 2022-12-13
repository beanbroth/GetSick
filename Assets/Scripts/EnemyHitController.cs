using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHitController : MonoBehaviour, IHittable
{
    [SerializeField] Rigidbody _rb;
    [SerializeField] float _knockbackMult;
    [SerializeField] PhysicsBasedEnemy _pbe;
    [SerializeField] float _ragDollTime;
    void Start()
    {
        
    }


    public void MainHit(Vector3 direction, Vector3 pos)
    {

        Debug.Log("owchie");
        _rb.AddForceAtPosition(direction * _knockbackMult, pos);
        _rb.angularVelocity *= 0.3f;

        JSAM.AudioManager.PlaySound(JSAM.Sounds.Hits);
        StartCoroutine(RagdollCooldown());

       
    }

    IEnumerator RagdollCooldown()
    {
        _pbe.ragdoll = true;
        yield return new WaitForSecondsRealtime(_ragDollTime);
        _pbe.ragdoll = false;
    }


    // Update is called once per frame
    void Update()
    {
        
    }



    private void OnCollisionEnter(Collision collision)
    {
        JSAM.AudioManager.PlaySound(JSAM.Sounds.Falls);
    }
}
