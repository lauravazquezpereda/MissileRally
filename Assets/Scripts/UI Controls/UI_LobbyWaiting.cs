using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_LobbyWaiting : MonoBehaviour
{
    // Se hace que este script sea un Singleton
    public static UI_LobbyWaiting instance;
    // Campo de texto en el canvas que muestra el c�digo de la sala
    [SerializeField] TMP_Text lobbyCode;
    // N�mero de jugadores de la sala
    public int NUM_JUGADORES = 0;
    // M�ximo de jugadores que puede haber
    const int MAX_JUGADORES = 4;
    // Lista de im�genes con los cuadros en los que se ir�n colocando los jugadores seg�n se vayan uniendo al Lobby
    [SerializeField] List<Image> cuadrosJugadores;
    // Lista con los nombres de los jugadores unidos a la sala
    [SerializeField] List<TMP_Text> nombresJugadores;
    // Lista de sprites con todos los posibles colores que podr�n tomar los cuadros de los jugadores, en funci�n del color del coche que hayan escogido
    [SerializeField] List<Sprite> listaColores;
    // Se indica si se ha creado la sala o no
    private bool salaCreada = false;
    // Referencia al propio canvas, al canvas anterior y al bot�n
    [SerializeField] GameObject canvasLobbyWaiting;
    [SerializeField] GameObject canvasLobby;
    [SerializeField] GameObject botonComenzar;
    // Texto de espera que se mostrar� cuando un cliente intente iniciar sin que el host haya tomado la decisi�n
    public GameObject textoEsperaHost;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }
    async void OnEnable()
    {
        // Se espera a que el sem�foro libere un permiso para continuar -> esto ocurrir� cuando se haya creado la sala
        await TestLobby.Instance.semaforoCreacionLobby.WaitAsync();
        // Una vez se haya creado la sala, se podr� mostrar su c�digo
        lobbyCode.text = "Codigo: " + TestLobby.Instance.GetCode();
        salaCreada = true;
    }

    private void Update()
    {
        // Si a�n no hay sala, no hay estado que actualizar
        if (!salaCreada) return;
        // Continuamente se muestran los jugadores unidos al lobby, junto con el color de su coche reflejado en el recuadro
        MostrarJugadoresLobby();
        // Si hay dos o m�s jugadores unidos, se puede comenzar la partida, por lo que se muestra el bot�n
        if (NUM_JUGADORES >= 2)
        {
            botonComenzar.SetActive(true);
        }
        else
        {
            botonComenzar.SetActive(false);
        }
    }
    // Esta funci�n se encarga de actualizar los recuadros en los que se colocan los nombres de los jugadores continuamente
    private void MostrarJugadoresLobby()
    {
        // Si no se ha unido a ninguna sala, no se ejecuta nada
        if(TestLobby.Instance.joinedLobby == null) return;
        // Para ello, se obtiene toda la informaci�n de los jugadores del lobby
        Dictionary<string, List<string>> datosJugadores = TestLobby.Instance.GetPlayersInLobby();
        datosJugadores.TryGetValue("Nombres", out List<string> nombres);

        NUM_JUGADORES = nombres.Count;
        // Se muestra el nombre de los jugadores conectados al lobby
        for (int i=0; i<NUM_JUGADORES; i++) {
            nombresJugadores[i].text = nombres[i];      
        }
        // Se limpia el nombre de los huecos sin asignar
        for (int i=NUM_JUGADORES; i<MAX_JUGADORES; i++)
        {
            nombresJugadores[i].text = "---------";
        }
        // Se modifica el color del recuadro de cada jugador, en funci�n del color del coche que haya escogido
        datosJugadores.TryGetValue("Colores", out List<string> colores);
        for(int i=0; i<NUM_JUGADORES; i++)
        {
            string color = colores[i];
            switch(color)
            {
                case "red":
                    cuadrosJugadores[i].sprite = listaColores[0];
                    break;
                case "yellow":
                    cuadrosJugadores[i].sprite = listaColores[1];
                    break;
                case "green":
                    cuadrosJugadores[i].sprite = listaColores[2];
                    break;
                case "blue":
                    cuadrosJugadores[i].sprite = listaColores[3];
                    break;
                case "orange":
                    cuadrosJugadores[i].sprite = listaColores[4];
                    break;
            }
        }
        // Se limpian los huecos en los que no hay ning�n jugador
        for(int i = NUM_JUGADORES; i<MAX_JUGADORES; i++)
        {
            cuadrosJugadores[i].sprite = listaColores[5];
        }
    }
    // Esta funci�n se ejecuta cuando se pulse el bot�n de abandonar la sala
    public async void AbandonarSala()
    {
        // Se vuelve a la pantalla anterior, pero antes, se hace que el jugador abandone el lobby, con la funci�n en TestLobby
        canvasLobby.SetActive(true);
        await TestLobby.Instance.LeaveLobby();
        canvasLobbyWaiting.SetActive(false);
    }
    // El script TestLobby se encarga de gestionar si se comienza el juego, se espera, se inicia como host o como cliente, al pulsar el bot�n de iniciar
    public void IniciarPartida()
    {
        TestLobby.Instance.StartGame();
    }

}
