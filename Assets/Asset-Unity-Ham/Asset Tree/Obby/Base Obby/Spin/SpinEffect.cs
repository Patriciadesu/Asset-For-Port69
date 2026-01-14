using UnityEngine;
using System.Collections.Generic;

public class SpinEffect : ObjectEffect
{
    public enum AxisOption { X, Y, Z, Custom }

    [SerializeField] private AxisOption axis = AxisOption.Y;
    [SerializeField] private Vector3 customAxis = Vector3.up;
    [SerializeField] private float rotationSpeed = 90f;
    private HashSet<GameObject> playersOnPlatform = new HashSet<GameObject>();

    private Vector3 GetRotationAxis()
    {
        return axis switch
        {
            AxisOption.X => Vector3.right,
            AxisOption.Y => Vector3.up,
            AxisOption.Z => Vector3.forward,
            AxisOption.Custom => customAxis,
            _ => Vector3.up
        };
    }

    private Vector3 RotatePointAround(Vector3 point, Vector3 pivot, Vector3 axis, float angle)
    {
        return Quaternion.AngleAxis(angle, axis) * (point - pivot) + pivot;
    }

    private void Update()
    {
        Vector3 rotationAxis = GetRotationAxis();
        float angle = rotationSpeed * Time.deltaTime;
        foreach (var player in playersOnPlatform)
        {
            if (player != null)
            {
                player.transform.position = RotatePointAround(player.transform.position, transform.position, rotationAxis, angle);
                player.transform.Rotate(rotationAxis, angle, Space.World);
            }
        }
        transform.Rotate(rotationAxis, angle);
    }

    public override void ApplyEffect(GameObject player)
    {
        playersOnPlatform.Add(player);
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersOnPlatform.Remove(other.gameObject);
        }
    }

    public void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playersOnPlatform.Remove(collision.gameObject);
        }
    }
}
