using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawnManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    void OnPlayerJoined(PlayerInput player)
    {
        int index = player.playerIndex;
        if (spawnPoints != null && index < spawnPoints.Length)
            player.transform.position = spawnPoints[index].position;
    }
}