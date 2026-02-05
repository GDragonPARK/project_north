using UnityEngine;
using System.Collections;

public class PooledParticle : MonoBehaviour
{
    public string poolTag;
    public float lifetime = 2f;

    private void OnEnable()
    {
        StartCoroutine(DisableAfterTime());
    }

    IEnumerator DisableAfterTime()
    {
        yield return new WaitForSeconds(lifetime);
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            gameObject.SetActive(false); // Fallback
        }
    }
}
