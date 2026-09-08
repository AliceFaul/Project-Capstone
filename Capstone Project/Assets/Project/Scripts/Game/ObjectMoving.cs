using System;
using UnityEngine;

public class ObjectMoving : MonoBehaviour
{
    [SerializeField] private float speed = 1f;

    private void FixedUpdate()
    {
        transform.position += transform.forward * (speed * Time.deltaTime);
    }
}