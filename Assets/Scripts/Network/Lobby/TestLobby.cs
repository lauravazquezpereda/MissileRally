using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class TestLobby : MonoBehaviour
{
    public static TestLobby Instance;

    Lobby hostLobby;
    Lobby joinedLobby;

    string lobbyName = "MissileRally";
    const int MAX_PLAYERS = 4;
    float heartBeatLobbyTimer = 0;
    const int MAX_HEARTBEAT_TIMER = 15;

    string playerHostName = "playerHost";
    string playerColor = "yellow";

    public bool salaCreada = false;

    // Creación de un semáforo para gestionar que no se muestre el código de la sala por pantalla, hasta que no se haya terminado de crear la propia sala
    public SemaphoreSlim semaforoCreacionLobby = new (0);

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }

    // Start is called before the first frame update
    private async void Start()
    {
        await UnityServices.InitializeAsync(); // Se espera hasta que se ejecuta la función de inicializar los servicios


        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Jugador registrado: " + AuthenticationService.Instance.PlayerId);
        };

    }

    private void Update()
    {
        if(hostLobby!= null)
        {
            HandleLobbyHeartbeat();
        }
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

    public async void CreateLobby()
    {
        try
        {
            // Es necesario autenticarse para poder crear la sala
            await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Se hace un registro anónimo, para no tener que guardar credenciales de cada jugador

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

            semaforoCreacionLobby.Release();

        } catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void JoinLobby(string code)
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Se hace un registro anónimo, para no tener que guardar credenciales de cada jugador
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
            semaforoCreacionLobby.Release();
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    // Con esta función, el jugador abandona la sala
    public async void LeaveLobby()
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

    // Función para modificar el nombre del jugador

    public void ModifyNamePlayer(string newName)
    {
        playerHostName = newName;
    }

    // Función para modificar el color del jugador

    public void ModifyColor(string color)
    {
        playerColor = color;
    }

    public string GetCode()
    {
        return joinedLobby.LobbyCode;
    }

    // Función para obtener los datos de los jugadores en el lobby y poder mostrarlos
    public Dictionary<string, List<string>> GetPlayersInLobby()
    {
        // Se crea un diccionario de listas de strings, para almacenar los nombres y los colores escogidos por los jugadores
        Dictionary<string, List<string>> datosPlayers = new Dictionary<string, List<string>>();
        // Se crea la lista de colores y se van añadiendo los colores de cada uno de los jugadores
        List<string> colores = new List<string>();
        foreach(Player p in joinedLobby.Players)
        {
            colores.Add(p.Data["CarColor"].Value);
        }
        // Se añade la lista al diccionario
        datosPlayers.Add("Colores", colores);
        // Se crea la lista de nombres y se van añadiendo los nombres de cada uno de los jugadores
        List<string> nombres = new List<string>();
        foreach(Player p in joinedLobby.Players)
        {
            nombres.Add(p.Data["Name"].Value);
        }
        // Se añade la lista al diccionario
        datosPlayers.Add("Nombres", nombres);

        return datosPlayers;
    }
    
}
