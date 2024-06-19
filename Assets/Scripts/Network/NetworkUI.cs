using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        UI_Circuit.instance.MostrarMenu();
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        UI_Circuit.instance.MostrarMenu();
    }

    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
    }

}
