using UnityEngine;

public class LayerChanger : MonoBehaviour
{
    public Transform player; // Reference to the player object
    public float changeDistance = 5f; // Distance within which to change the layer
    

    private Renderer[] renderers;
    private string targetLayerName = "Obstacle"; // The name of the layer to change to
    private string originalLayerName = "Floor 2"; // The name of the original layer to revert to
    private int targetLayer; // Integer representation of the target layer
    private int originalLayer; // Integer representation of the original layer

    void Start()
    {
        // Get all renderers of the object
        renderers = GetComponentsInChildren<Renderer>();

        // Convert layer names to layer indices
        targetLayer = LayerMask.NameToLayer(targetLayerName);
        originalLayer = LayerMask.NameToLayer(originalLayerName);
    }

    void Update()
    {
        CheckPlayerDistance();
    }

    void CheckPlayerDistance()
    {
        // Calculate the distance to the player
        float distanceToPlayer = Vector3.Distance(player.position, transform.position);

        // Change layer based on distance
        if (distanceToPlayer <= changeDistance)
        {
            ChangeLayer(targetLayer);
        }
        else
        {
            ChangeLayer(originalLayer);
        }
    }

    void ChangeLayer(int layer)
    {
        foreach (var renderer in renderers)
        {
            // Change the layer of the GameObject
            renderer.gameObject.layer = layer;
        }
    }
}
