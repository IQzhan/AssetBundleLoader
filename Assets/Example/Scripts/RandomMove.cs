using UnityEngine;

public class RandomMove : MonoBehaviour
{
    public Vector3 min;

    public Vector3 max;

    public float during = 0.1f;

    private float currTime;

    private Vector3 targetPos;

    private void Update()
    {
        if(Time.time - currTime > during)
        {
            currTime = Time.time;
            targetPos = new Vector3(
                    Random.Range(min.x, max.x),
                    Random.Range(min.y, max.y),
                    Random.Range(min.z, max.z)
                    );
        }
        transform.position = Vector3.Lerp(transform.position, targetPos, during);
    }
}
