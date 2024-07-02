using System.Collections.Generic;
using UnityEngine;

public class RaceController : MonoBehaviour 
{
    // Esta clase al ser un gestor, necesita ser accesible desde distintos scripts, por lo que ser� un Singleton
    public static RaceController instance;
    // Variable que almacena el n�mero de jugadores de la carrera
    public int numPlayers;
    // Lista de jugadores, que se ordena seg�n la posici�n de carrera
    public List<PlayerNetwork> _players = new(4);
    // Referencia al controlador del circuito
    public CircuitController _circuitController;
    // Esferas que siguen la l�nea que recorre el circuito y se utilizan para determinar la posici�n de carrera
    public GameObject[] _debuggingSpheres;
    // Variable que se utiliza para cambiar el comportamiento del juego en funci�n de si se est� en clasificaci�n o no
    public bool clasificacion = true;
    // Se indica si la carrera ha comenzado
    public bool carreraIniciada = false;
    // Y si ya ha sido preparada
    public bool carreraPreparada = false;
    // Lista que almacena las posiciones de los jugadores, �nicamente con sus identificadores, ya no con su componente Player completo
    public List<int> posiciones = new(4);

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        // Se asigna la referencia al controlador del circuito
        if (_circuitController == null) _circuitController = GetComponent<CircuitController>();

        // Se generan las esferas que recorren el circuito
        _debuggingSpheres = new GameObject[GameManager.Instance.numPlayers];
        for (int i = 0; i < GameManager.Instance.numPlayers; ++i)
        {
            _debuggingSpheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _debuggingSpheres[i].GetComponent<SphereCollider>().enabled = false;
            // Hacer la esfera invisible desactivando el componente MeshRenderer
            _debuggingSpheres[i].GetComponent<MeshRenderer>().enabled = false;
        }
    }

    private void Update()
    {
        // Si no hay jugadores, no se actualiza nada en el estado de la carrera
        if (_players.Count == 0)
            return;
        // Si se ha iniciado la carrera, se prepara
        if(carreraIniciada && !carreraPreparada)
        {
            carreraPreparada = true;
            // Se inicializa el circuito, una vez ya se ha escogido cu�l ser�
            _circuitController.StartCircuit();
            // Se modifica el color de los coches, para que el del resto de ls jugadores tambi�n sea seg�n su informaci�n en el Lobby
            ModificarColorCoches();
        } 
        if(carreraPreparada)
        {
            // Si ya se ha preparado la carrera, se actualiza su estado y se obtienen las posiciones ordenadas de los jugadores
            UpdateRaceProgress();
            OrderPositions();
        }
    }
    // Esta funci�n se invoca cada vez que spawnea un jugador
    public void AddPlayer(PlayerNetwork player)
    {
        // Se a�ade a la lista
        _players.Add(player);
        // Se incrementa el n�mero de jugadores
        numPlayers++;
    }
    // Esta clase es el comparador que se utiliza para ordenar la lista de jugadores en funci�n de su posici�n

    private class PlayerInfoComparer : Comparer<PlayerNetwork>
    {
        readonly float[] _arcLengths;

        public PlayerInfoComparer(float[] arcLengths)
        {
            _arcLengths = arcLengths;
        }
        // Se implementa el m�todo Compare en funci�n del n�mero de vueltas y cu�l se encuentra m�s adelantado en la misma vuelta
        public override int Compare(PlayerNetwork x, PlayerNetwork y)
        {
            // Va en una posici�n inferior aquel que lleva menos vueltas
            if (x.CurrentLap < y.CurrentLap)
            {
                return 1;
            }
            else if (x.CurrentLap > y.CurrentLap)
            {
                return -1;
            }
            // Si est�n en la misma vuelta, se comprueba en qu� posici�n se encuentran
            else
            {
                if (_arcLengths[x.ID] < _arcLengths[y.ID])
                {
                    return 1;
                }     
                else return -1;
            }

        }
    }
    // Esta funci�n ordena los jugadores en la lista en funci�n de su posici�n
    public void UpdateRaceProgress()
    {

        if (_players.Count == 0)
            return;

        // Update car arc-lengths
        float[] arcLengths = new float[_players.Count];

        for (int i = 0; i < _players.Count; ++i)
        {
            if (_players[i] == null)
            {
                return;
            }
            arcLengths[_players[i].ID] = ComputeCarArcLength(i);
        }


        _players.Sort(new PlayerInfoComparer(arcLengths)); // Se ordenan los jugadores

        string myRaceOrder = "";
        foreach (var player in _players)
        {
            myRaceOrder += player.Name + " ";
        }

        Debug.Log("Race order: " + myRaceOrder);
    }

    float ComputeCarArcLength(int id)
    {
        // Compute the projection of the car position to the closest circuit 
        // path segment and accumulate the arc-length along of the car along
        // the circuit.
        Vector3 carPos = this._players[id].car.transform.position;


        float minArcL =
            this._circuitController.ComputeClosestPointArcLength(carPos, out _, out var carProj, out _);

        this._debuggingSpheres[id].transform.position = carProj;

        if (this._players[id].CurrentLap == 0)
        {
            minArcL -= _circuitController.CircuitLength;
        }
        else
        {
            minArcL += _circuitController.CircuitLength *
                       (_players[id].CurrentLap - 1);
        }

        return minArcL;
    }
    // Esta funci�n se utiliza para corregir el color de los coches
    public void ModificarColorCoches()
    {
        // Para ello se necesita la informaci�n almacenada en el lobby, etiquetada en el diccionario con Colores
        Dictionary<string, List<string>> datosJugadores = TestLobby.Instance.GetPlayersInLobby();
        datosJugadores.TryGetValue("Colores", out List<string> colores);
        // A cada jugador se le asigna su color correspondiente
        for (int i = 0; i < TestLobby.Instance.NUM_PLAYERS_IN_LOBBY; i++)
        {
            Debug.Log(colores[_players[i].ID]);
            string color = colores[_players[i].ID]; // Se obtiene el color del ID del jugador, debido a que est�n ordenados de esta forma en la lista
            switch (color)
            {
                case "red":
                    _players[i].SetColor(0);
                    break;
                case "yellow":
                    _players[i].SetColor(1);
                    break;
                case "green":
                    _players[i].SetColor(2);
                    break;
                case "blue":
                    _players[i].SetColor(3);
                    break;
                case "orange":
                    _players[i].SetColor(4);
                    break;
            }
        }
    }

    public void OrderPositions()
    {
        // Con esta funci�n se obtienen los ID de los jugadores en orden de posici�n
        // Esto se va a utilizar para implementar la funcionalidad de Rubber Band
        posiciones.Clear();
        foreach (PlayerNetwork player in _players)
        {
            posiciones.Add(player.ID);
        }
    }
}