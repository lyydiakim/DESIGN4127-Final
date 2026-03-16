using UnityEngine;

public class GamesMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void goToGame(int id)
    {
        Application.LoadLevel(id);
    }

    public void quitApplication()
    {
        Application.Quit();
    }
}
