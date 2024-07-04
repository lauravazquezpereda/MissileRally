using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance;
    // Código para unirse al Relay
    public string joinCode;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }
    // Función que ejecuta el Host para crear el Relay
    public async Task CreateRelay(int maxPlayers)
    {
        try
        {
            // Se crea un lugar al que se pueden conectar los jugadores. Se reserva espacio para uno menos, ya que el host no cuenta
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            // De esta forma, se obtiene el código del relay, para poder unirse después
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            // Se obtiene la información del servidor, utilizando la localización reservada
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            // Se establece dicha información en el protocolo de transporte
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        } catch (RelayServiceException ex)
        {
            Debug.LogException(ex);
        }
    }
    // Función que ejecutan los Clientes para unirse al Relay, utilizando la clave
    public async void JoinRelay(string code)
    {
        try
        {
            // Se obtiene una referencia de la ubicación reservada
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);
            // Se obtiene la información del servidor, utilizando la localización reservada
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            // Se establece dicha información en el protocolo de transporte
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            // Se comienza el juego como cliente
            NetworkManager.Singleton.StartClient();

        } catch (RelayServiceException ex)
        {
            Debug.LogException(ex);
        }
    }
}
