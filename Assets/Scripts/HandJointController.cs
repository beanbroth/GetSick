using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandJointController : MonoBehaviour
{
    [SerializeField] SpringJoint _sj;

    [SerializeField] Transform _targHand;
    [SerializeField] Transform _headPos;

    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _sj.connectedAnchor = _targHand.position - _headPos.position;
    }
}
