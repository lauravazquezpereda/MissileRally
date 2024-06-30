using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NuevoCircuito : MonoBehaviour
{
    [SerializeField] GameObject canvasFinalJuego;
    [SerializeField] GameObject canvasCircuito;
    [SerializeField] GameObject HUD_Clasificacion;
    [SerializeField] GameObject HUD_Carrera;
    [SerializeField] GameObject textoEsperaHost;
    [SerializeField] GameObject canvasLobbyWaiting;
    [SerializeField] GameObject canvasSoloFin;
    [SerializeField] GameObject textoEspera;
    public async void VolverCircuitoAsync()
    {
        // Dependiendo de si el jugador es el host o es un cliente
        // El primer jugador del lobby será el host
        var hostPlayer = TestLobby.Instance.joinedLobby.Players[0];

        if (AuthenticationService.Instance.PlayerId == hostPlayer.Id)
        {
            // Actualizar el lobby para indicar que el juego ha comenzado
            var data = new Dictionary<string, PlayerDataObject>
            {
                { "HostStarted", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "true") }
            };

            var options = new UpdatePlayerOptions
            {
                Data = data
            };

            await LobbyService.Instance.UpdatePlayerAsync(TestLobby.Instance.joinedLobby.Id, AuthenticationService.Instance.PlayerId, options);

            canvasFinalJuego.SetActive(false);
            canvasCircuito.SetActive(true);
            HUD_Clasificacion.SetActive(true);
            HUD_Carrera.SetActive(false);
            textoEsperaHost.SetActive(false);
            UI_Circuit.instance.ResetState();
        }
        else
        {
            // Este jugador es un cliente
            // Verificar si el host ha iniciado el juego
            StartCoroutine(WaitForHostToStart());
        }

    }

    public void Volver()
    {
        textoEspera.SetActive(false);
        canvasSoloFin.SetActive(false);
        canvasLobbyWaiting.SetActive(true);
        HUD_Clasificacion.SetActive(true);
        HUD_Carrera.SetActive(false);
        textoEsperaHost.SetActive(false);
        NetworkManager.Singleton.Shutdown();
        // Se elimina el coche
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Destroy(player);
        UI_Circuit.instance.ResetState();
    }

    private IEnumerator WaitForHostToStart()
    {
        while (true)
        {
            if (TestLobby.Instance.joinedLobby.Players[0].Data.TryGetValue("HostStarted", out PlayerDataObject hostStarted) && hostStarted.Value == "true")
            {
                canvasFinalJuego.SetActive(false);
                canvasCircuito.SetActive(true);
                HUD_Clasificacion.SetActive(true);
                HUD_Carrera.SetActive(false);
                textoEsperaHost.SetActive(false);
                UI_Circuit.instance.ResetState();
                yield break;
            }
            else
            {
                Debug.Log("Esperando a que el host decida qué hacer.");
                textoEsperaHost.SetActive(true);
                yield return new WaitForSeconds(1f);
            }
        }
    }

}
