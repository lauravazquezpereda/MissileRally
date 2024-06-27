using System.Collections.Generic;
using UnityEngine;

public class RaceController : MonoBehaviour //determina mi orden de carrera
{
    public int numPlayers;

    [SerializeField] public List<PlayerNetwork> _players = new(4); //lista de jugadores
    public CircuitController _circuitController;
    public GameObject[] _debuggingSpheres; //esferas que acompañan 

    public static RaceController instance;

    public bool carreraIniciada = false;
    public bool carreraPreparada = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        if (_circuitController == null) _circuitController = GetComponent<CircuitController>();

        // generamos las esferas que necesitamos
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
        if (_players.Count == 0)
            return;

        if(carreraIniciada && !carreraPreparada)
        {
            carreraPreparada = true;
            _circuitController.StartCircuit();
            ModificarColorCoches();
        } 
        if(carreraPreparada)
        {
            UpdateRaceProgress();
        }
    }

    public void AddPlayer(PlayerNetwork player)
    {
        _players.Add(player);
        numPlayers++;
    }

    private class PlayerInfoComparer : Comparer<PlayerNetwork>
    {
        readonly float[] _arcLengths;

        public PlayerInfoComparer(float[] arcLengths)
        {
            _arcLengths = arcLengths;
        }

        public override int Compare(PlayerNetwork x, PlayerNetwork y)
        {
            // Va en una posición inferior aquel que lleva menos vueltas
            if (x.CurrentLap < y.CurrentLap)
            {
                return 1;
            }
            else if (x.CurrentLap > y.CurrentLap)
            {
                return -1;
            }
            // Si están en la misma vuelta, se comprueba en qué posición se encuentran
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

    public void UpdateRaceProgress()
    {
        // Update car arc-lengths
        float[] arcLengths = new float[_players.Count];

        for (int i = 0; i < _players.Count; ++i)
        {
            arcLengths[_players[i].ID] = ComputeCarArcLength(i);
        }


        _players.Sort(new PlayerInfoComparer(arcLengths)); //el orden de carrera sale de esta linea

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

    public void ModificarColorCoches()
    {
        // Corregir el color de los coches

        Dictionary<string, List<string>> datosJugadores = TestLobby.Instance.GetPlayersInLobby();
        datosJugadores.TryGetValue("Colores", out List<string> colores);

        for (int i = 0; i < TestLobby.Instance.NUM_PLAYERS_IN_LOBBY; i++)
        {
            string color = colores[i];
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
}