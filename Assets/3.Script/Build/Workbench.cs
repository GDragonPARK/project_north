using UnityEngine;
using System.Collections.Generic;

public class Workbench : MonoBehaviour
{
    public float range = 5f;
    public int level = 1;

    private static List<Workbench> allWorkbenches = new List<Workbench>();

    private void OnEnable()
    {
        allWorkbenches.Add(this);
    }

    private void OnDisable()
    {
        allWorkbenches.Remove(this);
    }

    /// <summary>
    /// Checks if a position is within range of any workbench
    /// </summary>
    public static bool IsInRange(Vector3 position, out Workbench closest)
    {
        closest = null;
        float minDist = float.MaxValue;

        foreach (var wb in allWorkbenches)
        {
            float dist = Vector3.Distance(position, wb.transform.position);
            if (dist <= wb.range && dist < minDist)
            {
                minDist = dist;
                closest = wb;
            }
        }

        return closest != null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
