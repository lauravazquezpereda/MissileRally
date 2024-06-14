using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_LobbyWaiting : MonoBehaviour
{
    [SerializeField] TMP_Text lobbyCode;
    public int NUM_JUGADORES = 0;
    [SerializeField] List<Image> cuadrosJugadores;
    [SerializeField] List<TMP_Text> nombresJugadores;
    [SerializeField] List<Sprite> listaColores;
    private bool salaCreada = false;

    // Start is called before the first frame update
    async void Start()
    {
        // Se espera a que el semáforo libere un permiso para continuar -> esto ocurrirá cuando se haya creado la sala
        await TestLobby.Instance.semaforoCreacionLobby.WaitAsync();
        lobbyCode.text = "Codigo: "+TestLobby.Instance.GetCode();
        salaCreada = true;
    }

    private void Update()
    {
        if (!salaCreada) return;
        MostrarJugadoresLobby();
    }

    private void MostrarJugadoresLobby()
    {
        Dictionary<string, List<string>> datosJugadores = TestLobby.Instance.GetPlayersInLobby();
        datosJugadores.TryGetValue("Nombres", out List<string> nombres);

        NUM_JUGADORES = nombres.Count;
        // Se muestra el nombre de los jugadores conectados al lobby
        for (int i=0; i<NUM_JUGADORES; i++) {
            nombresJugadores[i].text = nombres[i];      
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
    }
}
