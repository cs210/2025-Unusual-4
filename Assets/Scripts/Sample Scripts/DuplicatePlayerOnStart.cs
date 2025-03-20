using UnityEngine;

public class DuplicatePlayerOnStart : MonoBehaviour
{
    public GameObject playerPrefab; // Assign the Player prefab in the Inspector
    public Color duplicateColor = Color.red; // Change the color to whatever you prefer

    void Start()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab not assigned!");
            return;
        }

        // Instantiate the duplicate from the prefab at the same position and rotation as the original
        GameObject playerDuplicate = Instantiate(playerPrefab, playerPrefab.transform.position, playerPrefab.transform.rotation);

        // Change the color of the duplicate
        ChangePlayerColor(playerDuplicate, duplicateColor);
    }

    private void ChangePlayerColor(GameObject player, Color color)
    {
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (renderer.material != null)
            {
                renderer.material.color = color;
            }
        }
    }
}
