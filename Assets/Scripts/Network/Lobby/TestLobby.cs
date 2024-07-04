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
    // Esta clase se hace tambi�n un Singleton, ya que, es un gestor muy importante que almacena la informaci�n de los jugadores en las salas
    public static TestLobby Instance;
    // Referencia al lobby del host
    Lobby hostLobby;
    // Referencia p�blica al lobby de cada jugador, independientemente de si es host o client, para poder obtener informaci�n de ella (nombres, colores, inicio, n�mero de jugadores...)
    public Lobby joinedLobby;
    // Nombre para la sala
    string lobbyName = "MissileRally";
    // Se almacena el n�mero m�ximo de jugadores de la sala
    const int MAX_PLAYERS = 4;
    // Variables encargadas de hacer una pulsaci�n cada cierto tiempo, para que la sala no se destruya por inactividad
    float heartBeatLobbyTimer = 0;
    const int MAX_HEARTBEAT_TIMER = 15;
    // Variables que van a caracterizar al jugador en la partida
    string playerHostName = "playerHost"; // Nombre con el que va a aparecer, adem�s de ser identificado en la carrera
    string playerColor = "yellow"; // Color del coche que ha escogido para jugar
    // Variable que controla si la sala ya ha sido creada
    public bool salaCreada = false;
    // Variable que se utiliza para gestionar si el jugador est� en sala o no
    private bool enSala = false;
    // Creaci�n de un sem�foro para gestionar que no se muestre el c�digo de la sala por pantalla, hasta que no se haya terminado de crear la propia sala
    public SemaphoreSlim semaforoCreacionLobby = new(0);
    // Variable que se utiliza para limpiar las credenciales del jugador
    private bool jugadorRegistrado = false;
    // Mediante esta variable se controla que el juego se inicie una sola vez
    public bool juegoIniciado = false;
    // Variable p�blica accesible desde todos los dem�s scripts, que se utilizar para contar el n�mero de jugadores que hay conectados en el momento
    public int NUM_PLAYERS_IN_LOBBY;
    // Variable que almacena la clave del relay creado por el host
    public string relayCode;
    public string KEY_START_GAME = "relayCode"; // Key para el diccionario
    // Variable que se utiliza para indicar que se est� eliminando un jugador
    public bool jugadorEliminado = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

    }
    // Es asincr�nica, porque ejecuta m�todos as�ncronos
    private async void Start()
    {
        await UnityServices.InitializeAsync(); // Se espera hasta que se ejecuta la funci�n de inicializar los servicios
        // Se suscribe al evento que se invoca cuando se intenta cerrar la aplicaci�n, ya que es necesario hacer algunas gestiones antes de cerrar la aplicaci�n del todo
        Application.wantsToQuit += WantsToQuit;
        // Tambi�n se suscribe al evento de registro, con un delegado que muestra el ID con el que se asigna al jugador (de forma an�nima)
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Jugador registrado: " + AuthenticationService.Instance.PlayerId);
        };
        // Mediante esta corrutina, se actualiza cada cierto tiempo el estado del lobby, para tener siempre la informaci�n correcta (por si alguien se conecta o desconecta)
        StartCoroutine(UpdateLobby());
        // Se suscribe al m�todo de desconexi�n de un cliente, ya que tendr� que abandonar el lobby
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

    // Esta funci�n se utiliza para enviar un mensaje al lobby cada 15 segundos, para evitar que la sala desaparezca por inactividad
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
    // Esta funci�n se ejecuta cuando se pulsa el bot�n de crear una nueva sala en la UI
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
            // Si el jugador no ha sido registrado o s�, al ejecutar este m�todo ya se asume que as� ha sido
            jugadorRegistrado = true;
            enSala = true;

            // Definici�n de las propiedades del lobby
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = true,
                // DEFINICI�N DE LAS CARACTER�STICAS QUE VA A TENER CADA JUGADOR
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    // Se a�ade un campo para almacenar la clave del Relay
                    { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0") }
                }
            };
            // Creaci�n del objeto sala, con un nombre y un m�ximo de jugadores. Hasta que no se cree no se continua
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PLAYERS, options);
            // Se indica que es el lobby del host
            hostLobby = lobby;
            joinedLobby = hostLobby; // De esta forma, la referencia de la sala ser� la misma para el host y el resto de jugadores
            // Se muestra por consola la informaci�n del lobby
            Debug.Log("Lobby creado! " + lobby.Name + ", " + lobby.MaxPlayers + ", " + lobby.Id + ", " + lobby.LobbyCode);
            // Se muestra la informaci�n del jugador creador del lobby
            PrintPlayers(lobby);
            // Se libera un permiso que indica que ya se ha terminado la creaci�n del lobby, para poder continuar en la UI
            semaforoCreacionLobby.Release();

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    // Este m�todo se ejecuta cuando en la UI se introduce una clave
    public async Task JoinLobby(string code)
    {
        try
        {
            // Si el jugador no ha sido registrado, este se registra
            if (!jugadorRegistrado)
            {
                // Limpiar las credenciales de autenticaci�n antes de iniciar sesi�n, en caso de que se haya iniciado autom�ticamente sesi�n con las mismas credenciales
                AuthenticationService.Instance.ClearSessionToken();
                AuthenticationService.Instance.SignOut();
                await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Se hace un registro an�nimo, para no tener que guardar credenciales de cada jugador
            }
            // Se marca que as� ha sido
            jugadorRegistrado = true;
            enSala = true;
            // Se crea un jugador con las caracter�sticas correspondientes
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
            // Se libera un permiso en el sem�foro para que la UI pueda continuar
            semaforoCreacionLobby.Release();

        }
        catch (LobbyServiceException e)
        {
            // Se gestionan las excepciones que pueden ocurrir, para mostrarlas como texto en la UI
            // En caso de que el c�digo sea err�neo
            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                UI_Lobby.instance.MostrarError("No se ha encontrado ninguna sala con dicho c�digo");
            }
            // En caso de que la sala ya tenga su m�ximo de 4 participantes
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
            // Se limpian todas las referencias del c�digo
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
    // Se utiliza para obtener el c�digo de la sala
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
            // Cada segundo, se actualiza el estado del lobby
            yield return new WaitForSeconds(1f);

            if (joinedLobby != null)
            {
                // Se crea la tarea encargada de obtener el Lobby
                var tarea = GetLobby();
                // Mediante un delegado, se espera a que la tarea finalice para continuar
                yield return new WaitUntil(() => tarea.IsCompleted);
                // Se actualiza el n�mero de jugadores en la sala
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

    // Este m�todo se ejecuta cuando se intenta salir de la aplicaci�n
    private bool WantsToQuit()
    {
        // Si el jugador se encuentra en una sala, no se permite cerrar la aplicaci�n hasta que no haya salido de la sala
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
        // Se abandona la sala y despu�s finalmente se cierra la apliaci�n
        yield return LeaveLobby();
        enSala = false;
        Application.Quit();
    }

    // C�DIGO NECESARIO PARA INICIAR EL JUEGO UNA VEZ HAYA DOS O M�S JUGADORES
    public async void StartGame()
    {
        // Se evita que se pueda iniciar el juego varias veces
        if (juegoIniciado) return;
        juegoIniciado = true;

        // El primer jugador del lobby ser� el host
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
            // Se actualiza el lobby con dicha clave, para que al intentar iniciar, los jugadores no necesiten introducir este c�digo
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
            // Un cliente no puede tomar la decisi�n de iniciar el juego. Si le da al bot�n, aparecer� un mensaje que indica que se est� esperando la decisi�n del host
            // En caso de que el host ya haya decidido, comenzar� el juego
            // Se a�ade un par�metro m�s al jugador host, para que los clientes puedan hacer la comprobaci�n de si el host ha iniciado ya o no
            var data = new Dictionary<string, PlayerDataObject>
            {
                { "HostStarted", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "true") }
            };

            var options = new UpdatePlayerOptions
            {
                Data = data
            };
            // Se actualiza por lo tanto la informaci�n del host
            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, options);
            // Se muestra la siguiente pantalla, la selecci�n del circuito
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
            // Continuamente se comprueba si el Host ya ha iniciado el juego una vez se pulsa el bot�n, utilizando el campo que se a�ade cuando el host lo pulsa
            if (joinedLobby.Players[0].Data.TryGetValue("HostStarted", out PlayerDataObject hostStarted) && hostStarted.Value == "true")
            {
                // Comienza el juego como un cliente
                // Primero debe de unirse al Relay que haya creado el Host
                // Para hacerlo, primero debe conseguir la clave del Relay
                relayCode = joinedLobby.Data[KEY_START_GAME].Value;
                // Despu�s, se utiliza para unirse al Relay. Una vez hecho, se comienza el juego como cliente
                RelayManager.Instance.JoinRelay(relayCode);
                // Se muestra el men� de selecci�n de circuito
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
    // Esta funci�n se utiliza al terminar una carrera y querer empezar otra. Es el host el que debe decidir. Por lo tanto, se hace que el campo valga false hasta que el host le de
    // al bot�n correspondiente
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
    // Esta funci�n se ejecuta cuando un cliente se desconecta y se encarga de eliminarlo de la lista de jugadores de la carrera
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
        // Se decrementa el n�mero de jugadores
        RaceController.instance.numPlayers--;
        // Se comprueba si es el host quien se ha desconectado, para informar a los jugadores
        if(clientId == 0)
        {
            EndingController.Instance.FinalizarDesconexionHost();
        }
        // Se hace que termine la carrera por abandono, en caso de que queden m�s jugadores, informando de la situaci�n
        if(RaceController.instance.numPlayers > 1)
        {
            EndingController.Instance.FinalizarDesconexion();
        }
    }

}
