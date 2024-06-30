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

    public static EndingController Instance;
    public int corredoresRestantes;

    public bool carreraFinalizada = false;
    public List<int> ordenFinal;

    public int numPlayers;

    [SerializeField] GameObject canvasFinalAbandono;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }

    private void Update()
    {
        ConsultarPlayers();
    }

    [ServerRpc (RequireOwnership = false)]
    public void PrepararFinalServerRpc(int numJugadores)
    {
        numPlayers = numJugadores;
        corredoresRestantes = numJugadores - 1;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AvisarMetaServerRpc(int playerIndex)
    {
        corredoresRestantes--;
        // Se añade el índice del que ya ha terminado a la lista
        ordenFinal.Add(playerIndex);
        // Se oculta el coche del que ya ha terminado
        HidePlayerClientRpc(playerIndex);
        
        // Si ya no quedan corredores por llegar
        if(corredoresRestantes == 0)
        {

            // Se añade en el último puesto el corredor que no ha sido añadido aún a la lista
            int[] lista = new int[numPlayers];

            for(int i = 0; i < ordenFinal.Count; i++)
            {
                lista[ordenFinal[i]]++;
            }

            // Este es aquel que no ha sido añadido a la lista, pues no ha llegado aún a la meta
            for(int i = 0; i < numPlayers; i++)
            {
                if (lista[i] == 0)
                {
                    ordenFinal.Add(i); 
                    Debug.Log("Ultima posicion: " + i.ToString());
                }
            }

            // Se guarda todo en variables, para facilitar el proceso de envío al cliente, sin tener que serializar
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

            FloatArray tiemposVuelta = new FloatArray { Values = UI_HUD.Instance.tiemposVueltaJugadores };
            UI_HUD.Instance.MostrarResultadosCarreraClientRpc(pos1, pos2, pos3, pos4, tiemposVuelta);

        }
    }

    // Esta función se utiliza para ocultar un coche una vez ha terminado la carrera
    [ClientRpc]
    public void HidePlayerClientRpc(int playerId)
    {
        for(int i=0; i<RaceController.instance._players.Count; i++)
        {
            // Se oculta el coche en todos los clientes, excepto en el que ha terminado la carrera
            // De esta forma, se evita que los que aún están corriendo se choquen con quien ya ha terminado
            if (RaceController.instance._players[i].ID == playerId && !RaceController.instance._players[i].IsOwner)
            {
                RaceController.instance._players[i].car.SetActive(false);
            }
        }
        
    }

    // Esta función sirve para controlar que haya siempre más de un jugador en la partida
    private void ConsultarPlayers()
    {
        if(TestLobby.Instance.NUM_PLAYERS_IN_LOBBY == 1 && UI_Clasificacion.instance.inicioCarrera)
        {
            // Se limpia la lista
            RaceController.instance._players.Clear();
            carreraFinalizada = true;
            canvasFinalAbandono.SetActive(true);
            NetworkManager.Singleton.Shutdown();
        }
    }

}
