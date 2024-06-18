using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class TestLobby : MonoBehaviour
{
    public static TestLobby Instance;

    Lobby hostLobby;
    public Lobby joinedLobby;

    string lobbyName = "MissileRally";
    const int MAX_PLAYERS = 4;
    float heartBeatLobbyTimer = 0;
    const int MAX_HEARTBEAT_TIMER = 15;

    string playerHostName = "playerHost";
    string playerColor = "yellow";

    public bool salaCreada = false;
    private bool enSala = false;

    // Creaci�n de un sem�foro para gestionar que no se muestre el c�digo de la sala por pantalla, hasta que no se haya terminado de crear la propia sala
    public SemaphoreSlim semaforoCreacionLobby = new(0);

    private bool jugadorRegistrado = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

    }

    // Start is called before the first frame update
    private async void Start()
    {
        await UnityServices.InitializeAsync(); // Se espera hasta que se ejecuta la funci�n de inicializar los servicios

        Application.wantsToQuit += WantsToQuit;

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Jugador registrado: " + AuthenticationService.Instance.PlayerId);
        };

        StartCoroutine(UpdateLobby());
    }

    private void Update()
    {
        if (hostLobby != null)
        {
            HandleLobbyHeartbeat();
        }
    }

    // Esta funci�n se utiliza para enviar un mensaje al lobby cada 15 segundos, para evitar que la sala desaparezca por inactividad
    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartBeatLobbyTimer += Time.deltaTime;
            if (heartBeatLobbyTimer > MAX_HEARTBEAT_TIMER)
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
            if (!jugadorRegistrado)
            {
                // Es necesario autenticarse para poder crear la sala
                // Limpiar las credenciales de autenticaci�n antes de iniciar sesi�n, en caso de que se haya iniciado autom�ticamente sesi�n con las mismas credenciales
                AuthenticationService.Instance.ClearSessionToken();
                AuthenticationService.Instance.SignOut();
                await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Se hace un registro an�nimo, para no tener que guardar credenciales de cada jugador
            }

            jugadorRegistrado = true;
            enSala = true;

            // Definici�n de las propiedades del lobby
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = true,
                // DEFINICI�N DE LAS CARACTER�STICAS QUE VA A TENER CADA JUGADOR
                Player = GetPlayer()
            };
            // Creaci�n del objeto sala, con un nombre y un m�ximo de jugadores. Hasta que no se cree no se continua
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PLAYERS, options);

            hostLobby = lobby;
            joinedLobby = hostLobby; // De esta forma, la referencia de la sala ser� la misma para el host y el resto de jugadores

            Debug.Log("Lobby creado! " + lobby.Name + ", " + lobby.MaxPlayers + ", " + lobby.Id + ", " + lobby.LobbyCode);

            PrintPlayers(lobby);

            semaforoCreacionLobby.Release();

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async Task JoinLobby(string code)
    {
        try
        {
            if (!jugadorRegistrado)
            {
                // Limpiar las credenciales de autenticaci�n antes de iniciar sesi�n, en caso de que se haya iniciado autom�ticamente sesi�n con las mismas credenciales
                AuthenticationService.Instance.ClearSessionToken();
                AuthenticationService.Instance.SignOut();
                await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Se hace un registro an�nimo, para no tener que guardar credenciales de cada jugador
            }

            jugadorRegistrado = true;
            enSala = true;
            // Se crea un jugador con las caracter�sticas correspondientes
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
        catch (LobbyServiceException e)
        {
            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                UI_Lobby.instance.MostrarError("No se ha encontrado ninguna sala con dicho c�digo");
            }
            else if (e.Reason == LobbyExceptionReason.LobbyFull)
            {
                UI_Lobby.instance.MostrarError("La sala a la que se intenta acceder est� llena");
            }
            Debug.Log(e);
        }
    }

    // Con esta funci�n, el jugador abandona la sala
    public async Task LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
            Debug.Log("Jugador ha abandonado la sala");
            joinedLobby = null;
            hostLobby = null;
            enSala = false;
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
        foreach (Player p in l.Players)
        {
            Debug.Log(p.Id + " " + p.Data["Name"].Value + " " + p.Data["CarColor"].Value);
        }
    }

    // Funci�n para modificar el nombre del jugador

    public void ModifyNamePlayer(string newName)
    {
        playerHostName = newName;
    }

    // Funci�n para modificar el color del jugador

    public void ModifyColor(string color)
    {
        playerColor = color;
    }

    public string GetCode()
    {
        return joinedLobby.LobbyCode;
    }

    // Funci�n para obtener los datos de los jugadores en el lobby y poder mostrarlos
    public Dictionary<string, List<string>> GetPlayersInLobby()
    {
        // Se crea un diccionario de listas de strings, para almacenar los nombres y los colores escogidos por los jugadores
        Dictionary<string, List<string>> datosPlayers = new Dictionary<string, List<string>>();
        // Se crea la lista de colores y se van a�adiendo los colores de cada uno de los jugadores
        List<string> colores = new List<string>();
        foreach (Player p in joinedLobby.Players)
        {
            colores.Add(p.Data["CarColor"].Value);
        }
        // Se a�ade la lista al diccionario
        datosPlayers.Add("Colores", colores);
        // Se crea la lista de nombres y se van a�adiendo los nombres de cada uno de los jugadores
        List<string> nombres = new List<string>();
        foreach (Player p in joinedLobby.Players)
        {
            nombres.Add(p.Data["Name"].Value);
        }
        // Se a�ade la lista al diccionario
        datosPlayers.Add("Nombres", nombres);

        return datosPlayers;
    }

    // Funci�n para actualizar continuamente el lobby
    IEnumerator UpdateLobby()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (joinedLobby != null)
            {
                // Se crea la tarea encargada de obtener el Lobby
                var tarea = GetLobby();
                // Mediante un delegado, se espera a que la tarea finalice para continuar
                yield return new WaitUntil(() => tarea.IsCompleted);
            }
        }
    }

    async Task GetLobby()
    {
        try
        {
            // Mediante el ID de la sala, se obtiene una referencia actualizada
            Lobby lobbyActualizado = await Lobbies.Instance.GetLobbyAsync(joinedLobby.Id);
            joinedLobby = lobbyActualizado;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    // Este m�todo se ejecuta cuando se intenta salir de la aplicaci�n
    private bool WantsToQuit()
    {
        // Si el jugador se encuentra en una sala, no se permite cerrar la aplicaci�n hasta que no haya salido de la sala
        if(enSala)
        {
            StartCoroutine(LeaveLobbyApplication());
            return false;
        }
        else
        {
            return true;
        }
    }

    IEnumerator LeaveLobbyApplication()
    {
        // Se abandona la sala y despu�s finalmente se cierra la apliaci�n
        yield return LeaveLobby();
        enSala = false;
        Application.Quit();
    } 

}
