using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(-7f, 10f, -7f);
    [SerializeField] private float followSharpness = 12f;

    public void Configure(Transform player) { target = player; Snap(); }
    public void Snap()
    {
        if (target == null) return;
        transform.position = target.position + offset;
        transform.rotation = Quaternion.LookRotation(-offset, Vector3.up);
    }

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = Vector3.Lerp(transform.position, target.position + offset, followSharpness * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(-offset, Vector3.up);
    }
}
