using UnityEngine;
using UnityEngine.SceneManagement;
using MongoDB.Driver;

public class Menu : MonoBehaviour
{
    public void StartClicked()
    {
        Debug.Log("Start clicked");
        
        // TODO : Extract to singleton class.
        // Connect.
        string serverIp = "mongodb://125.209.192.227:27017"; // The server ip and port number.
        MongoClient client = new MongoClient(serverIp);
        
        SceneManager.LoadScene(1); // The InGame scene has the scene number 1.
    }

    public void ExitClicked()
    {
        Debug.Log("Exit clicked");

        Application.Quit();
    }
}
