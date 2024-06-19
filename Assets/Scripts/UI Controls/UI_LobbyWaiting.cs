using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_LobbyWaiting : MonoBehaviour
{
    public static UI_LobbyWaiting instance;

    [SerializeField] TMP_Text lobbyCode;
    public int NUM_JUGADORES = 0;
    const int MAX_JUGADORES = 4;
    [SerializeField] List<Image> cuadrosJugadores;
    [SerializeField] List<TMP_Text> nombresJugadores;
    [SerializeField] List<Sprite> listaColores;
    private bool salaCreada = false;
    [SerializeField] GameObject canvasLobbyWaiting;
    [SerializeField] GameObject canvasLobby;
    [SerializeField] GameObject botonComenzar;
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
        // Se espera a que el semáforo libere un permiso para continuar -> esto ocurrirá cuando se haya creado la sala
        await TestLobby.Instance.semaforoCreacionLobby.WaitAsync();
        lobbyCode.text = "Codigo: " + TestLobby.Instance.GetCode();
        salaCreada = true;
    }

    private void Update()
    {
        if (!salaCreada) return;
        MostrarJugadoresLobby();
        if (NUM_JUGADORES >= 2)
        {
            botonComenzar.SetActive(true);
        }
        else
        {
            botonComenzar.SetActive(false);
        }
    }

    private void MostrarJugadoresLobby()
    {
        if(TestLobby.Instance.joinedLobby == null) return;
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
        // Se modifica el color del recuadro de cada jugador, en función del color del coche que haya escogido
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
        // Se limpian los huecos en los que no hay ningún jugador
        for(int i = NUM_JUGADORES; i<MAX_JUGADORES; i++)
        {
            cuadrosJugadores[i].sprite = listaColores[5];
        }
    }

    public async void AbandonarSala()
    {
        canvasLobby.SetActive(true);
        await TestLobby.Instance.LeaveLobby();
        canvasLobbyWaiting.SetActive(false);
    }

    public void IniciarPartida()
    {
        TestLobby.Instance.StartGame();
    }

}
