using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI   ;
using UnityEngine.SceneManagement;

public class SimpleLobby : MonoBehaviour
{
    public string m_sceneOnGo;
    public GameObject[] m_players = new GameObject[4];
    public uint m_playerCountBeforeGo = 2;
    // Start is called before the first frame update
    void Start()
    {
        OnlineManager.Instance.RegisterHandler((byte)OnlineProtocol.Handler.LOBBY_GO, Recv);
    }

    // Update is called once per frame
    void Update()
    {
              
    }
    public void GoPressed()
    {
        if (OnlineManager.Instance.IsHost())
        {
            {
                Send();
                SceneManager.LoadScene(m_sceneOnGo, LoadSceneMode.Single);
            }
        }
    }

    private void Recv(byte[] _msg)
    {
        SceneManager.LoadScene(m_sceneOnGo, LoadSceneMode.Single);
    }
    private void Send()
    {
        OnlineManager.Instance.SendMessage((byte)OnlineProtocol.Handler.LOBBY_GO,new  byte[0]);

    }
}
