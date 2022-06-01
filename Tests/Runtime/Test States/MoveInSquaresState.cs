using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This is only met to be applied to only one object
public class MoveInSquaresState : SampleTransformBaseState
{
    public List<Vector3> positions = new List<Vector3>();
    public Vector3 startPos;
    public int length;
    public int index = 0;
    public float speed = 5;
    
    const float INVERSE_OF_ROOT_2 = 0.7071f;

    public override void OnEnd(Transform obj)
    {
        
    }

    public override void OnStart(Transform obj)
    {
        positions.Clear();
        positions.AddRange(new Vector3[]
        {
            new Vector3(-1, 0, -1),
            new Vector3(1, 0, -1),
            new Vector3(1, 0, 1),
            new Vector3(-1, 0, 1),
        });
        for(int i = 0; i < 4; i++)
        {
           
            positions[i] = positions[i] * INVERSE_OF_ROOT_2 * length;
        }
        
    }

    public override void OnUpdate(Transform obj)
    {
        var dir = (positions[index] - obj.transform.position).normalized;
        obj.transform.position += speed * Time.deltaTime * dir;
        if(Vector3.Distance(obj.transform.position, positions[index]) < 1f)
        {
            index = ++index % positions.Count;
        }
    }

    public override void OnGizmos(Transform obj)
    {
        for(int i = 0; i < positions.Count; i++)
        {
            var position = positions[i];
            Gizmos.color = index==i ? Color.red : Color.green;
            Gizmos.DrawWireSphere(position, 0.5f);
            Gizmos.DrawWireSphere(position, 0.5f);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(position, positions[(i + 1) % positions.Count]);
        }

    }
}
