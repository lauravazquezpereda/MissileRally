using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_Lobby : MonoBehaviour
{
    // Se hace que este script sea un Singleton para hacerlo accesible desde el resto de scripts
    public static UI_Lobby instance;
    // Referencia al peque�o men� en el que se permite introducir una clave para unirse a una sala
    [SerializeField] GameObject menuClave;
    // Este bot�n permanece invisible hasta que se introduce una clave
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
    // Funci�n que se ejecuta cuando se pulsa el bot�n para crear una sala
    public void CrearSala()
    {
        // Se oculta el canvas actual, se muestra el siguiente y se ejecuta la funci�n de crear sala del script TestLobby
        canvasLobbyWaiting.SetActive(true);
        TestLobby.Instance.CreateLobby();
        canvasLobby.SetActive(false);
    }
   // Funci�n que muestra el men� para introducir una clave, una vez se pulsa el bot�n de unirse a una sala
    public void UnirseSala()
    {
        menuClave.SetActive(true);
    }
    // Funci�n que oculta el men� de introducir una clave
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
    // Funci�n as�ncrona en la que se ejecuta la funci�n de unirse a un lobby de TestLobby con el c�digo introducido. Si ha dado alg�n error, no se contin�a y se muestra el texto
    // Si no, se pasa al siguiente men� una vez dentro de la sala
    public async void Unirse()
    {
        await TestLobby.Instance.JoinLobby(lobbyCode);
        if (errorLobby.text != "") return;      
        canvasLobbyWaiting.SetActive(true);
        canvasLobby.SetActive(false);
    }

    private void Update()
    {
        // Si se ha introducido una clave, se muestra el bot�n de unirse a un Lobby
        if(claveIntroducida)
        {
            botonUnirse.SetActive(true);
        }
    }
    // Se muestra el posible error al unirse a una sala con c�digo
    public void MostrarError(string text)
    {
        errorLobby.text = text;
    }

    public void OcultarError()
    {
        errorLobby.text = "";
    }

}
