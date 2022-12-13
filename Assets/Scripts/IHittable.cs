using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHittable
{
    public void MainHit(Vector3 direction, Vector3 position);
    //public void FirstHit(Vector2 direction, float dist);
}
