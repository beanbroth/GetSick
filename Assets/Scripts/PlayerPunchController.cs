using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPunchController : MonoBehaviour
{
    Vector3 previous;
    Vector3 velocity;
    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        velocity = ((transform.position - previous)) / Time.fixedDeltaTime;
        previous = transform.position;

        //Debug.Log(velocity.magnitude);

        if (velocity.magnitude > 7f && !JSAM.AudioManager.IsSoundPlaying(JSAM.Sounds.Woosh))
        {
            JSAM.AudioManager.PlaySound(JSAM.Sounds.Woosh);
        }
    }



    private void OnTriggerEnter(Collider other)
    {

        IHittable tempTarget = other.GetComponent<IHittable>();
        if (tempTarget != null)
        {
            tempTarget.MainHit(velocity, transform.position);
        }
    }
}
