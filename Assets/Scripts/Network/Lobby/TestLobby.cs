using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;

public class TestLobby : MonoBehaviour
{
    // Esta clase se hace también un Singleton, ya que, es un gestor muy importante que almacena la información de los jugadores en las salas
    public static TestLobby Instance;
    // Referencia al lobby del host
    Lobby hostLobby;
    // Referencia pública al lobby de cada jugador, independientemente de si es host o client, para poder obtener información de ella (nombres, colores, inicio, número de jugadores...)
    public Lobby joinedLobby;
    // Nombre para la sala
    string lobbyName = "MissileRally";
    // Se almacena el número máximo de jugadores de la sala
    const int MAX_PLAYERS = 4;
    // Variables encargadas de hacer una pulsación cada cierto tiempo, para que la sala no se destruya por inactividad
    float heartBeatLobbyTimer = 0;
    const int MAX_HEARTBEAT_TIMER = 15;
    // Variables que van a caracterizar al jugador en la partida
    string playerHostName = "playerHost"; // Nombre con el que va a aparecer, además de ser identificado en la carrera
    string playerColor = "yellow"; // Color del coche que ha escogido para jugar
    // Variable que controla si la sala ya ha sido creada
    public bool salaCreada = false;
    // Variable que se utiliza para gestionar si el jugador está en sala o no
    private bool enSala = false;
    // Creación de un semáforo para gestionar que no se muestre el código de la sala por pantalla, hasta que no se haya terminado de crear la propia sala
    public SemaphoreSlim semaforoCreacionLobby = new(0);
    // Variable que se utiliza para limpiar las credenciales del jugador
    private bool jugadorRegistrado = false;
    // Mediante esta variable se controla que el juego se inicie una sola vez
    public bool juegoIniciado = false;
    // Variable pública accesible desde todos los demás scripts, que se utilizar para contar el número de jugadores que hay conectados en el momento
    public int NUM_PLAYERS_IN_LOBBY;
    // Variable que almacena la clave del relay creado por el host
    public string relayCode;
    public string KEY_START_GAME = "relayCode"; // Key para el diccionario
    // Variable que se utiliza para indicar que se está eliminando un jugador
    public bool jugadorEliminado = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

    }
    // Es asincrónica, porque ejecuta métodos asíncronos
    private async void Start()
    {
        await UnityServices.InitializeAsync(); // Se espera hasta que se ejecuta la función de inicializar los servicios
        // Se suscribe al evento que se invoca cuando se intenta cerrar la aplicación, ya que es necesario hacer algunas gestiones antes de cerrar la aplicación del todo
        Application.wantsToQuit += WantsToQuit;
        // También se suscribe al evento de registro, con un delegado que muestra el ID con el que se asigna al jugador (de forma anónima)
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Jugador registrado: " + AuthenticationService.Instance.PlayerId);
        };
        // Mediante esta corrutina, se actualiza cada cierto tiempo el estado del lobby, para tener siempre la información correcta (por si alguien se conecta o desconecta)
        StartCoroutine(UpdateLobby());
        // Se suscribe al método de desconexión de un cliente, ya que tendrá que abandonar el lobby
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void Update()
    {
        // Si estamos unidos a un lobby, cada cierto tiempo se la mandan pulsaciones
        if (hostLobby != null)
        {
            HandleLobbyHeartbeat();
        }
    }

    // Esta función se utiliza para enviar un mensaje al lobby cada 15 segundos, para evitar que la sala desaparezca por inactividad
    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartBeatLobbyTimer += Time.deltaTime;
            // Si se supera el tiempo umbral sin enviar un mensaje, este se manda
            if (heartBeatLobbyTimer > MAX_HEARTBEAT_TIMER)
            {
                heartBeatLobbyTimer = 0;
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }
    // Esta función se ejecuta cuando se pulsa el botón de crear una nueva sala en la UI
    public async void CreateLobby()
    {
        try
        {
            if (!jugadorRegistrado)
            {
                // Es necesario autenticarse para poder crear la sala
                // Limpiar las credenciales de autenticación antes de iniciar sesión, en caso de que se haya iniciado automáticamente sesión con las mismas credenciales
                AuthenticationService.Instance.ClearSessionToken();
                AuthenticationService.Instance.SignOut();
                await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Se hace un registro anónimo, para no tener que guardar credenciales de cada jugador
            }
            // Si el jugador no ha sido registrado o sí, al ejecutar este método ya se asume que así ha sido
            jugadorRegistrado = true;
            enSala = true;

            // Definición de las propiedades del lobby
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = true,
                // DEFINICIÓN DE LAS CARACTERÍSTICAS QUE VA A TENER CADA JUGADOR
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    // Se añade un campo para almacenar la clave del Relay
                    { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0") }
                }
            };
            // Creación del objeto sala, con un nombre y un máximo de jugadores. Hasta que no se cree no se continua
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PLAYERS, options);
            // Se indica que es el lobby del host
            hostLobby = lobby;
            joinedLobby = hostLobby; // De esta forma, la referencia de la sala será la misma para el host y el resto de jugadores
            // Se muestra por consola la información del lobby
            Debug.Log("Lobby creado! " + lobby.Name + ", " + lobby.MaxPlayers + ", " + lobby.Id + ", " + lobby.LobbyCode);
            // Se muestra la información del jugador creador del lobby
            PrintPlayers(lobby);
            // Se libera un permiso que indica que ya se ha terminado la creación del lobby, para poder continuar en la UI
            semaforoCreacionLobby.Release();

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    // Este método se ejecuta cuando en la UI se introduce una clave
    public async Task JoinLobby(string code)
    {
        try
        {
            // Si el jugador no ha sido registrado, este se registra
            if (!jugadorRegistrado)
            {
                // Limpiar las credenciales de autenticación antes de iniciar sesión, en caso de que se haya iniciado automáticamente sesión con las mismas credenciales
                AuthenticationService.Instance.ClearSessionToken();
                AuthenticationService.Instance.SignOut();
                await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Se hace un registro anónimo, para no tener que guardar credenciales de cada jugador
            }
            // Se marca que así ha sido
            jugadorRegistrado = true;
            enSala = true;
            // Se crea un jugador con las características correspondientes
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };
            // Se quiere unir a la primera sala, se espera hasta que esto ocurra
            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(code, options);
            // Se guarda la referencia al lobby
            joinedLobby = lobby;

            Debug.Log("Joined Lobby with code: " + code);
            // Se imprimen por consola todos los jugadores en sala
            PrintPlayers(lobby);
            // Se libera un permiso en el semáforo para que la UI pueda continuar
            semaforoCreacionLobby.Release();

        }
        catch (LobbyServiceException e)
        {
            // Se gestionan las excepciones que pueden ocurrir, para mostrarlas como texto en la UI
            // En caso de que el código sea erróneo
            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                UI_Lobby.instance.MostrarError("No se ha encontrado ninguna sala con dicho código");
            }
            // En caso de que la sala ya tenga su máximo de 4 participantes
            else if (e.Reason == LobbyExceptionReason.LobbyFull)
            {
                UI_Lobby.instance.MostrarError("La sala a la que se intenta acceder está llena");
            }
            Debug.Log(e);
        }
    }

    // Con esta función, el jugador abandona la sala
    public async Task LeaveLobby()
    {
        try
        {
            // Se limpian todas las referencias del código
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
        // Se crea un nuevo jugador identificado por el nombre y el color que haya escogido al principio
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
    // Se obtienen los jugadores del lobby y se muestran por pantalla
    private void PrintPlayers(Lobby l)
    {
        Debug.Log("Players in lobby ------ ");
        foreach (Player p in l.Players)
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
    // Se utiliza para obtener el código de la sala
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
        foreach (Player p in joinedLobby.Players)
        {
            colores.Add(p.Data["CarColor"].Value);
        }
        // Se añade la lista al diccionario
        datosPlayers.Add("Colores", colores);
        // Se crea la lista de nombres y se van añadiendo los nombres de cada uno de los jugadores
        List<string> nombres = new List<string>();
        foreach (Player p in joinedLobby.Players)
        {
            nombres.Add(p.Data["Name"].Value);
        }
        // Se añade la lista al diccionario
        datosPlayers.Add("Nombres", nombres);

        return datosPlayers;
    }

    // Función para actualizar continuamente el lobby
    IEnumerator UpdateLobby()
    {
        while (true)
        {
            // Cada segundo, se actualiza el estado del lobby
            yield return new WaitForSeconds(1f);

            if (joinedLobby != null)
            {
                // Se crea la tarea encargada de obtener el Lobby
                var tarea = GetLobby();
                // Mediante un delegado, se espera a que la tarea finalice para continuar
                yield return new WaitUntil(() => tarea.IsCompleted);
                // Se actualiza el número de jugadores en la sala
                if(joinedLobby != null)
                {
                    NUM_PLAYERS_IN_LOBBY = joinedLobby.Players.Count;
                }
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

    // Este método se ejecuta cuando se intenta salir de la aplicación
    private bool WantsToQuit()
    {
        // Si el jugador se encuentra en una sala, no se permite cerrar la aplicación hasta que no haya salido de la sala
        if(enSala)
        {
            // Se inicia una corrutina encargada de hacer que el jugador salga de la sala
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
        // Se abandona la sala y después finalmente se cierra la apliación
        yield return LeaveLobby();
        enSala = false;
        Application.Quit();
    }

    // CÓDIGO NECESARIO PARA INICIAR EL JUEGO UNA VEZ HAYA DOS O MÁS JUGADORES
    public async void StartGame()
    {
        // Se evita que se pueda iniciar el juego varias veces
        if (juegoIniciado) return;
        juegoIniciado = true;

        // El primer jugador del lobby será el host
        var hostPlayer = joinedLobby.Players[0];

        // Si nos encontramos por lo tanto en la build en la que el ID coincide con el ID obtenido para el host, se inicia el juego como Host
        if (AuthenticationService.Instance.PlayerId == hostPlayer.Id)
        {
            // Este jugador es el host
            // Antes de iniciarse, se crea el espacio de Relay, reservado para 4 jugadores incluyendo al Host
            // Se espera a que se cree para continuar
            await RelayManager.Instance.CreateRelay(MAX_PLAYERS);
            // Una vez creado se obtiene la clave
            relayCode = RelayManager.Instance.joinCode;
            // Se actualiza el lobby con dicha clave, para que al intentar iniciar, los jugadores no necesiten introducir este código
            Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                    {
                        KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode)
                    } }
            });
            // Se actualiza la referencia del lobby actual
            joinedLobby = lobby;

            // Una vez hecho, se inicia el Host
            NetworkManager.Singleton.StartHost();

            // Actualizar el lobby para indicar que el juego ha comenzado
            // Un cliente no puede tomar la decisión de iniciar el juego. Si le da al botón, aparecerá un mensaje que indica que se está esperando la decisión del host
            // En caso de que el host ya haya decidido, comenzará el juego
            // Se añade un parámetro más al jugador host, para que los clientes puedan hacer la comprobación de si el host ha iniciado ya o no
            var data = new Dictionary<string, PlayerDataObject>
            {
                { "HostStarted", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "true") }
            };

            var options = new UpdatePlayerOptions
            {
                Data = data
            };
            // Se actualiza por lo tanto la información del host
            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, options);
            // Se muestra la siguiente pantalla, la selección del circuito
            UI_Circuit.instance.MostrarMenu();
        }
        else
        {
            // Este jugador es un cliente
            // Verificar si el host ha iniciado el juego
            StartCoroutine(WaitForHostToStart());
        }
    }

    private IEnumerator WaitForHostToStart()
    {
        while (true)
        {
            // Continuamente se comprueba si el Host ya ha iniciado el juego una vez se pulsa el botón, utilizando el campo que se añade cuando el host lo pulsa
            if (joinedLobby.Players[0].Data.TryGetValue("HostStarted", out PlayerDataObject hostStarted) && hostStarted.Value == "true")
            {
                // Comienza el juego como un cliente
                // Primero debe de unirse al Relay que haya creado el Host
                // Para hacerlo, primero debe conseguir la clave del Relay
                relayCode = joinedLobby.Data[KEY_START_GAME].Value;
                // Después, se utiliza para unirse al Relay. Una vez hecho, se comienza el juego como cliente
                RelayManager.Instance.JoinRelay(relayCode);
                // Se muestra el menú de selección de circuito
                UI_Circuit.instance.MostrarMenu();
                yield break;
            }
            else
            {
                // Se muestra el texto de espera
                Debug.Log("Esperando a que el host inicie el juego.");
                UI_LobbyWaiting.instance.textoEsperaHost.SetActive(true);
                yield return new WaitForSeconds(1f);
            }
        }
    }
    // Esta función se utiliza al terminar una carrera y querer empezar otra. Es el host el que debe decidir. Por lo tanto, se hace que el campo valga false hasta que el host le de
    // al botón correspondiente
    public async void ReiniciarEsperaHost()
    {
        // Se actualiza el campo de inicio a false
        var data = new Dictionary<string, PlayerDataObject>
            {
                { "HostStarted", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "false") }
            };

        var options = new UpdatePlayerOptions
        {
            Data = data
        };

        await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, options);
    }
    // Esta función se ejecuta cuando un cliente se desconecta y se encarga de eliminarlo de la lista de jugadores de la carrera
    public void OnClientDisconnected(ulong clientId)
    {
        jugadorEliminado = true;
        // Primero se buscan todas las componentes nulas de la lista de jugadores, para eliminarlas
        int idNulo = 0;
        for(int i=0; i<RaceController.instance._players.Count; i++)
        {
            if (RaceController.instance._players[i] == null) {
                idNulo = i;
            }
        }
        RaceController.instance._players.Remove(RaceController.instance._players[idNulo]);
        // Se decrementa el número de jugadores
        RaceController.instance.numPlayers--;
        // Se comprueba si es el host quien se ha desconectado, para informar a los jugadores
        if(clientId == 0)
        {
            EndingController.Instance.FinalizarDesconexionHost();
        }
        // Se hace que termine la carrera por abandono, en caso de que queden más jugadores, informando de la situación
        if(RaceController.instance.numPlayers > 1)
        {
            EndingController.Instance.FinalizarDesconexion();
        }
    }

}
