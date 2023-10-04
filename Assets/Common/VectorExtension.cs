using UnityEngine;

public static class VectorExtension
{
    public static Vector3 ElementProduct(this Vector3 lhs, Vector3 rhs)
    {
        return new Vector3(
            lhs.x * rhs.x,
            lhs.y * rhs.y,
            lhs.z * rhs.z
        );
    }

    public static Vector3 ElementAbs(this Vector3 it)
    {
        return new Vector3(
            Mathf.Abs(it.x),
            Mathf.Abs(it.y),
            Mathf.Abs(it.z)
        );
    }

    public static Vector3 ElementAverage(this Vector3[] it)
    {
        Vector3 ret = Vector3.zero;
        foreach (Vector3 element in it)
        {
            ret += element;
        }

        return ret / it.Length;
    }
}