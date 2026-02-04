using UnityEngine;

public class ConstructionGhost : MonoBehaviour
{
    public Material greenMat;
    public Material redMat;
    public bool canBuild = true;
    private MeshRenderer meshRenderer;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        // Simple check for overlapping (could be more complex with triggers/colliders)
        Collider[] colliders = Physics.OverlapBox(transform.position, transform.localScale / 2);
        canBuild = true;
        foreach (var col in colliders)
        {
            if (col.gameObject != gameObject && !col.isTrigger)
            {
                canBuild = false;
                break;
            }
        }

        meshRenderer.material = canBuild ? greenMat : redMat;
    }
}
