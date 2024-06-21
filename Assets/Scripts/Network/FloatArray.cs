using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

// Mediante esta estructura, se va a poder serializar el array de floats que almacena los tiempos de los jugadores
public struct FloatArray : INetworkSerializable
{
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
