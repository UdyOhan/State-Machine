using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveInCircles : SampleTransformBaseState
{
    public float length = 20;
    public List<Vector3> points;
    public int noOfPoints = 20;
    public float speed = 10;
    public int index;
    public bool rotatePoints;
    public float rotateAngularSpeed = 5;
    public override void OnEnd(Transform obj)
    {
        
    }

    public override void OnGizmos(Transform obj)
    {
        for(int i = 0; i < points.Count; i++)
        {
            var point = points[i];
            Gizmos.color = index == i ? Color.red : Color.green;
            Gizmos.DrawSphere(point, 0.5f);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(point, points[(i + 1) % points.Count]);
        }

    }

    public override void OnStart(Transform obj)
    {
        points.Clear();
        index = 0;
        float radDiv = 360 / noOfPoints;
        Vector3 startPoint = new Vector3(-length/2, 0, 0);
        startPoint = Quaternion.Euler(0, 45f, 0) * startPoint;
        points.Add(startPoint);
        for(int i = 1; i < noOfPoints; i++)
        {
            Debug.Log(i - 1);
            points.Add(Quaternion.Euler(0, radDiv, 0) * points[i - 1]);
        }
      
    }

    public override void OnUpdate(Transform obj)
    {
        var dir = (points[index] - obj.transform.position).normalized;
        obj.transform.position += speed * Time.deltaTime * dir;
        obj.forward = Vector3.Lerp(obj.forward, dir, Time.deltaTime * 8f);
        if (Vector3.Distance(obj.transform.position, points[index]) < 1f)
        {
            index = ++index % points.Count;
        }

        if (rotatePoints)
        {
            for(int i = 0; i < points.Count; i++)
            {
                points[i] = Quaternion.Euler(0, rotateAngularSpeed * Time.deltaTime, 0) * points[i];
            }
        }
    }
}
