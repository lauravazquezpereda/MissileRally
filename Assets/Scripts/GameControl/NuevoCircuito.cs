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
    // Este script se utiliza para controlar lo que sucede despu�s de terminar una carrera y querer comenzar otra, o al quedarse solo y querer volver al lobby
    // Por lo tanto, se utiliza una referencia a todas las pantallas del canvas involucradas, para poder ocultar unas y mostrar otras, en funci�n del caso
    [SerializeField] GameObject canvasFinalJuego;
    [SerializeField] GameObject canvasCircuito;
    [SerializeField] GameObject HUD_Clasificacion;
    [SerializeField] GameObject HUD_Carrera;
    [SerializeField] GameObject textoEsperaHost;
    [SerializeField] GameObject canvasLobbyWaiting;
    [SerializeField] GameObject canvasSoloFin;
    [SerializeField] GameObject textoEspera;

    // Esta funci�n se ejecuta al finalizar una carrera y querer comenzar otra, d�ndole al bot�n de volver en el canvas de los resultados
    public async void VolverCircuitoAsync()
    {
        // Dependiendo de si el jugador es el host o es un cliente
        // El primer jugador del lobby ser� el host
        var hostPlayer = TestLobby.Instance.joinedLobby.Players[0];

        if (AuthenticationService.Instance.PlayerId == hostPlayer.Id)
        {
            // Actualizar el lobby para indicar que el host ya ha decidido
            var data = new Dictionary<string, PlayerDataObject>
            {
                { "HostStarted", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "true") }
            };

            var options = new UpdatePlayerOptions
            {
                Data = data
            };
            // Se actualiza la informaci�n del host con este nuevo campo
            await LobbyService.Instance.UpdatePlayerAsync(TestLobby.Instance.joinedLobby.Id, AuthenticationService.Instance.PlayerId, options);
            // Se desactiva el canvas con los resultados
            canvasFinalJuego.SetActive(false);
            // Se muestra la pantalla de selecci�n del circuito
            canvasCircuito.SetActive(true);
            // Se hace visible el HUD de la clasificaci�n, para que se muestre despu�s de seleccionar el circuito
            HUD_Clasificacion.SetActive(true);
            // Se hace invisible el HUD de la carrera, que ya se mostrar� tras terminar la clasificaci�n
            HUD_Carrera.SetActive(false);
            // Se quita el texto de espera al host, por si estaba visible
            textoEsperaHost.SetActive(false);
            // Se resetea el estado de la selecci�n del circuito y todo lo pertinente a la carrera, para poder jugar otra sin que influyan los datos anteriores
            UI_Circuit.instance.ResetState();
        }
        else
        {
            // Este jugador es un cliente
            // Verificar si el host ha iniciado el juego
            StartCoroutine(WaitForHostToStart());
        }

    }

    // Esta funci�n se ejecuta cuando un jugador se queda solo y quiere volver al lobby desde el canvas que se lo indica
    public void Volver()
    {
        // Se oculta el texto de espera del canvas del lobby
        textoEspera.SetActive(false);
        // Se desactiva el canvas de final
        canvasSoloFin.SetActive(false);
        // Se vuelve a mostrar el canvas de espera en el lobby
        canvasLobbyWaiting.SetActive(true);
        // Se hace visible el HUD de la clasificaci�n
        HUD_Clasificacion.SetActive(true);
        // Se hace invisible el HUD de la carrera, que se mostrar� tras terminar con la clasificaci�n
        HUD_Carrera.SetActive(false);
        // Se quita el texto de espera al host
        textoEsperaHost.SetActive(false);
        // Se tira el cliente o el servidor, ya que no hay ning�n juego iniciado tras esto
        NetworkManager.Singleton.Shutdown();
        // Se elimina el coche
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Destroy(player);
        // Se resetea el estado del circuito
        UI_Circuit.instance.ResetState();
    }
    // De la misma forma que esperando para que el host iniciara la partida, en este caso el cliente espera hasta que el host decida volver al men� de selecci�n de circuito
    // para comenzar una nueva carrera
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
                Debug.Log("Esperando a que el host decida qu� hacer.");
                textoEsperaHost.SetActive(true);
                yield return new WaitForSeconds(1f);
            }
        }
    }

}
