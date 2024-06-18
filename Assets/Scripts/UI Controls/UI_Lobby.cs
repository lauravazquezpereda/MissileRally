using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_Lobby : MonoBehaviour
{
    public static UI_Lobby instance;

    [SerializeField] GameObject menuClave;
    [SerializeField] GameObject botonUnirse;
    [SerializeField] GameObject canvasLobby;
    [SerializeField] GameObject canvasLobbyWaiting;
    [SerializeField] TMP_Text errorLobby;

    string lobbyCode;
    bool claveIntroducida = false;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }
    private void OnEnable()
    {
        errorLobby.text = "";
    }
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

    public async void Unirse()
    {
        await TestLobby.Instance.JoinLobby(lobbyCode);
        if (errorLobby.text != "") return;      
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
    public void MostrarError(string text)
    {
        errorLobby.text = text;
    }

    public void OcultarError()
    {
        errorLobby.text = "";
    }

}
