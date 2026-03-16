using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class resourceManagerCanvas : MonoBehaviour
{

    public GameObject timerModule;
    public GameObject goalTrackerModule;
    //public GameObject globalScoreModule;
    public TMP_Text globalScoreModule;

    //game states:
    public GameObject pauseScreen;
    public GameObject endOfChallengeScreeen;

    public List<GameObject> playerJoinPanels;

    public playersInfo playerInfoManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerInfoManager = GameObject.FindAnyObjectByType<playersInfo>();
    }

    // Update is called once per frame
    void Update()
    {
        updatePlayerInvites();
    }

    public void updatePlayerInvites()
    {
        if (playerInfoManager != null)
        {
            for (int i = 0; i < playerJoinPanels.Count; i++)
            {
                if (i >= playerInfoManager.allPlayers.Count)
                {
                    playerJoinPanels[i].SetActive(true);
                } else
                {
                    playerJoinPanels[i].SetActive(false);
                }
            }
        }
    }
}
