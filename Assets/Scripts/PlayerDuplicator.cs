using UnityEngine;

public class PlayerDuplicator : MonoBehaviour
{
    public GameObject playerPrefab;

    void Start()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab is not assigned in the PlayerDuplicator script.");
            return;
        }

        // Duplicate the player
        GameObject newPlayer = Instantiate(playerPrefab);
        newPlayer.name = "PlayerDuplicate";
    }
}
