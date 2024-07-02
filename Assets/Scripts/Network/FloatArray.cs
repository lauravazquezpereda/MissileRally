using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;

// En este script se crean dos estructuras, que se pueden serializar para enviarlos en peticiones Rpc

// Esta estructura se utiliza para poder transmitir un array de floats, que se usa para enviar todos los tiempos de las vueltas almacenados en el servidor, a los clientes
public struct FloatArray : INetworkSerializable
{
    // Para acceder a los elementos, se utiliza el atributo Values
    public float[] Values;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int length = Values != null ? Values.Length : 0;
        serializer.SerializeValue(ref length);

        if (serializer.IsReader)
        {
            Values = new float[length];
        }
        // Se va serializando cada valor del array proporcionado como parámetro
        for (int i = 0; i < length; i++)
        {
            serializer.SerializeValue(ref Values[i]);
        }
    }
}
// Esta estructura se utiliza para transmitir la lista con las posiciones de los jugadores tras la clasificación
public struct IntList : INetworkSerializable
{
    // Para acceder a los elementos, se utiliza el atributo Values
    public List<int> Values;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int length = Values != null ? Values.Count : 0;
        serializer.SerializeValue(ref length);

        if (serializer.IsReader)
        {
            Values = new List<int>(length);
        }

        for (int i = 0; i < length; i++)
        {
            int value = Values != null && i < Values.Count ? Values[i] : 0;
            serializer.SerializeValue(ref value);

            if (serializer.IsReader)
            {
                Values.Add(value);
            }
        }
    }
}
