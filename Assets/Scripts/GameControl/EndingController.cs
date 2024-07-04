using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class EndingController : NetworkBehaviour
{
    // ESTE SCRIPT VA A SER UTILIZADO PARA CONTROLAR EL FINAL DE LA CARRERA
    // Se necesita que sea un Singleton, porque va a ser accesible desde varios scripts diferentes
    public static EndingController Instance;

    // Esta variable va a almacenar el n�mero de corredores que quedan para llegar a la meta
    public int corredoresRestantes;
    // Esta variable va a almacenar el n�mero de corredores que han llegado a la meta
    public int corredoresLlegados;
    // Se debe proteger, ya que, distintos procesos pueden intentar acceder a esta variable a la vez (variable compartida)
    // Para ello se utiliza exclusi�n mutua:
    Object cerrojoMeta = new Object(); // Cerrojo para la variable compartida

    // Variable booleana que indica que ha terminado la carrera
    public bool carreraFinalizada = false;
    // Lista que almacena el orden en el que han llegado los jugadores a la meta
    public List<int> ordenFinal;
    // N�mero de jugadores
    public int numPlayers;
    // Referencia al canvas que muestra que un jugador se ha quedado solo
    [SerializeField] GameObject canvasFinalAbandono;
    // Referencia al canvas que muestra que un jugador se ha desconectado en mitad de la carrera
    [SerializeField] GameObject canvasFinalDesconexion;
    // Referencia al canvas que muestra que el host se ha desconectado en mitad de la carrera
    [SerializeField] GameObject canvasFinalDesconexionHost;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }

    private void Update()
    {
        // Desde este script se comprueba si hay s�lo un jugador, para terminar el juego
        ConsultarPlayers();
        // Adem�s, se prepara continuamente el final, es decir, los jugadores que tienen que llegar de forma din�mica
        PrepararFinal();
    }

    private void PrepararFinal()
    {
        numPlayers = TestLobby.Instance.NUM_PLAYERS_IN_LOBBY;
        corredoresRestantes = numPlayers - 1;
    }
   
    [ServerRpc(RequireOwnership = false)]
    public void AvisarMetaServerRpc(int playerIndex)
    {
        // Se protege la variable de corredores restantes para que no se produzcan errores de concurrencia, si varios clientes mandan ejecutar la funci�n a la vez al pasar al mismo tiempo por la meta
        lock(cerrojoMeta)
        {
            corredoresLlegados++;
            // Se a�ade el �ndice del que ya ha terminado a la lista
            ordenFinal.Add(playerIndex);
        }

        // Se oculta el coche del que ya ha terminado
        HidePlayerClientRpc(playerIndex);
        
        // Si ya no quedan corredores por llegar
        if(corredoresRestantes == corredoresLlegados)
        {

            // Se a�ade en el �ltimo puesto el corredor que no ha sido a�adido a�n a la lista
            // Para ello, se comprueba cu�l es aquel que no ha llegado a la meta
            int[] lista = new int[numPlayers];

            for(int i = 0; i < ordenFinal.Count; i++)
            {
                lista[ordenFinal[i]]++;
            }

            // Este es aquel que no ha sido a�adido a la lista, pues no ha llegado a�n a la meta
            for(int i = 0; i < numPlayers; i++)
            {
                if (lista[i] == 0)
                {
                    ordenFinal.Add(i); 
                    Debug.Log("Ultima posicion: " + i.ToString());
                }
            }

            // Se guarda todo en variables, para facilitar el proceso de env�o al cliente, sin tener que serializar
            // Se dejan inicializadas en -1, para que, si al recibirlas hay alg�n -1, quiere decir que no hay ning�n jugador en dicha posici�n, por lo que no hay nada que mostrar
            int pos1 = -1, pos2 = -1, pos3 = -1, pos4 = -1;

            for(int i = 0; i < ordenFinal.Count; i++)
            {
                switch(i)
                {
                    case 0:
                        pos1 = ordenFinal[i]; break;
                    case 1:
                        pos2 = ordenFinal[i]; break;
                    case 2:
                        pos3 = ordenFinal[i]; break;
                    case 3:
                        pos4 = ordenFinal[i]; break;

                }
            }
            // Se serializan los tiempos por vuelta para que cada cliente muestre los suyos y el tiempo total
            FloatArray tiemposVuelta = new FloatArray { Values = UI_HUD.Instance.tiemposVueltaJugadores };
            // Se env�an los datos a una funci�n que se ejecuta en todos los clientes
            UI_HUD.Instance.MostrarResultadosCarreraClientRpc(pos1, pos2, pos3, pos4, tiemposVuelta);

        }
    }

    // Esta funci�n se utiliza para ocultar un coche una vez ha terminado la carrera
    [ClientRpc]
    public void HidePlayerClientRpc(int playerId)
    {
        for(int i=0; i<RaceController.instance._players.Count; i++)
        {
            // Se oculta el coche en todos los clientes, excepto en el que ha terminado la carrera
            // De esta forma, se evita que los que a�n est�n corriendo se choquen con quien ya ha terminado
            if (RaceController.instance._players[i].ID == playerId && !RaceController.instance._players[i].IsOwner)
            {
                RaceController.instance._players[i].car.SetActive(false);
            }
        }        
    }

    // Esta funci�n sirve para controlar que haya siempre m�s de un jugador en la partida
    private void ConsultarPlayers()
    {
        // Se necesita haber comenzado la carrera para realizar esta comprobaci�n
        if(TestLobby.Instance.NUM_PLAYERS_IN_LOBBY == 1 && UI_Clasificacion.instance.inicioCarrera)
        {
            // Se limpia la lista y se indica que hay 0 jugadores ya
            RaceController.instance._players.Clear();
            RaceController.instance.numPlayers = 0;
            carreraFinalizada = true;
            // Se muestra la pantalla en la que se indica que has ganado porque los dem�s han abandonado
            canvasFinalAbandono.SetActive(true);
            NetworkManager.Singleton.Shutdown();
        }
    }

    // Esta funci�n se ejecuta cuando alg�n cliente se desconecta en mitad de la partida, dejando a los dem�s perdidos
    public void FinalizarDesconexion()
    {
        // Se limpia la lista y se indica que hay 0 jugadores ya
        RaceController.instance._players.Clear();
        RaceController.instance.numPlayers = 0;
        carreraFinalizada = true;
        // Se muestra la pantalla en la que se indica que un jugador se ha desconectado
        canvasFinalDesconexion.SetActive(true);
        NetworkManager.Singleton.Shutdown();
    }

    // Esta funci�n se ejecuta cuando el host se desconecta en mitad de la partida
    public void FinalizarDesconexionHost()
    {
        // Se limpia la lista y se indica que hay 0 jugadores ya
        RaceController.instance._players.Clear();
        RaceController.instance.numPlayers = 0;
        carreraFinalizada = true;
        // Se muestra la pantalla en la que se indica que un jugador se ha desconectado
        canvasFinalDesconexionHost.SetActive(true);
        NetworkManager.Singleton.Shutdown();
    }

}
