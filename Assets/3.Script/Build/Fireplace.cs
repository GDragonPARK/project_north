using UnityEngine;
using System.Collections.Generic;

public class Fireplace : MonoBehaviour
{
    public float warmthRadius = 5f;
    private static List<Fireplace> allFireplaces = new List<Fireplace>();

    private void OnEnable() => allFireplaces.Add(this);
    private void OnDisable() => allFireplaces.Remove(this);

    public static bool IsNearFire(Vector3 position)
    {
        foreach (var fire in allFireplaces)
        {
            if (Vector3.Distance(position, fire.transform.position) <= fire.warmthRadius)
                return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, warmthRadius);
    }
}
