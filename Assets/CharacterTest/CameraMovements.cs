using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovements : MonoBehaviour
{
    // Attach this to camera 
    public Vector3 focusPosition;
    public Vector3 targetPosition;
    public Vector3 targetRotation;
    public bool startMove;
    public bool startRotate;
    public float speed;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (startMove)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed*Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.1)
            {
                Debug.Log("Stop Moving");
                startMove = false;
            }
        }

        if (startRotate)
        {
             Vector3 relativePos = focusPosition - transform.position;
             Quaternion toRotation = Quaternion.LookRotation(relativePos);
             transform.rotation = Quaternion.Lerp( transform.rotation, toRotation, Mathf.SmoothStep(0,1, speed * Time.deltaTime));
        }
    }
}
