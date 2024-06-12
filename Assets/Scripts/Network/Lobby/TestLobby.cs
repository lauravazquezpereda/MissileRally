using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class TestLobby : MonoBehaviour
{
    Lobby hostLobby;
    Lobby joinedLobby;

    string lobbyName = "MissileRally";
    const int MAX_PLAYERS = 4;
    float heartBeatLobbyTimer = 0;
    const int MAX_HEARTBEAT_TIMER = 15;

    string playerHostName = "playerHost";
    string playerColor = "yellow";


    // Start is called before the first frame update
    private async void Start()
    {
        await UnityServices.InitializeAsync(); // Se espera hasta que se ejecuta la función de inicializar los servicios


        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Jugador registrado: " + AuthenticationService.Instance.PlayerId);
        };


        await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Se hace un registro anónimo, para no tener que guardar credenciales de cada jugador

    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.W)) 
        {
            CreateLobby();
        }
        if(Input.GetKeyDown(KeyCode.S))
        {
            // JoinLobby();
        }
        HandleLobbyHeartbeat();
    }

    // Esta función se utiliza para enviar un mensaje al lobby cada 15 segundos, para evitar que la sala desaparezca por inactividad
    private async void HandleLobbyHeartbeat()
    {
        if(hostLobby!= null)
        {
            heartBeatLobbyTimer += Time.deltaTime;
            if(heartBeatLobbyTimer > MAX_HEARTBEAT_TIMER)
            {
                heartBeatLobbyTimer = 0;
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    private async void CreateLobby()
    {
        try
        {
            // Definición de las propiedades del lobby
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = true,
                // DEFINICIÓN DE LAS CARACTERÍSTICAS QUE VA A TENER CADA JUGADOR
                Player = GetPlayer()
            };
            // Creación del objeto sala, con un nombre y un máximo de jugadores. Hasta que no se cree no se continua
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PLAYERS, options);

            hostLobby = lobby;
            joinedLobby = hostLobby; // De esta forma, la referencia de la sala será la misma para el host y el resto de jugadores

            Debug.Log("Lobby creado! " + lobby.Name + ", " + lobby.MaxPlayers + ", "+ lobby.Id + ", "+lobby.LobbyCode);

            PrintPlayers(lobby);

        } catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void JoinLobby(string code)
    {
        try
        {
            // Se crea un jugador con las características correspondientes
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };
            // Se quiere unir a la primera sala, se espera hasta que esto ocurra
            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(code, options);
            joinedLobby = lobby;

            Debug.Log("Joined Lobby with code: " + code);

            PrintPlayers(lobby);
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    // Con esta función, el jugador abandona la sala
    private async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
                    {
                        // Solo almacenamos el nombre del jugador, junto con el color del coche que lleve
                        { "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerHostName) },
                        { "CarColor", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerColor) }
                    }
        };
    }

    private void PrintPlayers(Lobby l)
    {
        Debug.Log("Players in lobby ------ ");
        foreach(Player p in l.Players)
        {
            Debug.Log(p.Id + " " + p.Data["Name"].Value + " " + p.Data["CarColor"].Value);
        }
    }




    
}
