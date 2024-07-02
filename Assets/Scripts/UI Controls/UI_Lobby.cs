using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_Lobby : MonoBehaviour
{
    // Se hace que este script sea un Singleton para hacerlo accesible desde el resto de scripts
    public static UI_Lobby instance;
    // Referencia al pequeño menú en el que se permite introducir una clave para unirse a una sala
    [SerializeField] GameObject menuClave;
    // Este botón permanece invisible hasta que se introduce una clave
    [SerializeField] GameObject botonUnirse;
    // Referencia al propio canvas para poder ocultarlo
    [SerializeField] GameObject canvasLobby;
    // Referencia al canvas de espera en la propia sala
    [SerializeField] GameObject canvasLobbyWaiting;
    // Texto que muestra el posible error al unirse a una sala
    [SerializeField] TMP_Text errorLobby;

    string lobbyCode;
    bool claveIntroducida = false; // Variable que controla si ya se ha introducido una clave

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
    // Función que se ejecuta cuando se pulsa el botón para crear una sala
    public void CrearSala()
    {
        // Se oculta el canvas actual, se muestra el siguiente y se ejecuta la función de crear sala del script TestLobby
        canvasLobbyWaiting.SetActive(true);
        TestLobby.Instance.CreateLobby();
        canvasLobby.SetActive(false);
    }
   // Función que muestra el menú para introducir una clave, una vez se pulsa el botón de unirse a una sala
    public void UnirseSala()
    {
        menuClave.SetActive(true);
    }
    // Función que oculta el menú de introducir una clave
    public void Volver()
    {
        menuClave.SetActive(false);
    }
    // Se ejecuta cuando se introduce una clave en el campo de texto
    public void IntroducirClave(string code)
    {
        claveIntroducida = true;
        lobbyCode = code;
    }
    // Función asíncrona en la que se ejecuta la función de unirse a un lobby de TestLobby con el código introducido. Si ha dado algún error, no se continúa y se muestra el texto
    // Si no, se pasa al siguiente menú una vez dentro de la sala
    public async void Unirse()
    {
        await TestLobby.Instance.JoinLobby(lobbyCode);
        if (errorLobby.text != "") return;      
        canvasLobbyWaiting.SetActive(true);
        canvasLobby.SetActive(false);
    }

    private void Update()
    {
        // Si se ha introducido una clave, se muestra el botón de unirse a un Lobby
        if(claveIntroducida)
        {
            botonUnirse.SetActive(true);
        }
    }
    // Se muestra el posible error al unirse a una sala con código
    public void MostrarError(string text)
    {
        errorLobby.text = text;
    }

    public void OcultarError()
    {
        errorLobby.text = "";
    }

}
