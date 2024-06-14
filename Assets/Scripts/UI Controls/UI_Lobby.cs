using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Lobby : MonoBehaviour
{
    [SerializeField] GameObject menuClave;
    [SerializeField] GameObject botonUnirse;
    [SerializeField] GameObject canvasLobby;
    [SerializeField] GameObject canvasLobbyWaiting;

    string lobbyCode;
    bool claveIntroducida = false;

    public void CrearSala()
    {
        canvasLobbyWaiting.SetActive(true);
        TestLobby.Instance.CreateLobby();
        canvasLobby.SetActive(false);
    }

    public void UnirseSala()
    {
        menuClave.SetActive(true);
    }

    public void Volver()
    {
        menuClave.SetActive(false);
    }

    public void IntroducirClave(string code)
    {
        claveIntroducida = true;
        lobbyCode = code;
    }

    public void Unirse()
    {
        TestLobby.Instance.JoinLobby(lobbyCode);
        canvasLobbyWaiting.SetActive(true);
        canvasLobby.SetActive(false);
    }

    private void Update()
    {
        if(claveIntroducida)
        {
            botonUnirse.SetActive(true);
        }
    }

}
