using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct GameData : INetworkSerializable, IEquatable<GameData>
{
    public ulong clientID; //0 ~ 1
    public FixedString32Bytes playerName;
    public bool ready;
    public ushort colorIdx; //슬라임 컬러

    public bool Equals(GameData other)
    {
        return clientID == other.clientID && playerName.Equals(other.playerName);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientID);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref ready);
        serializer.SerializeValue(ref colorIdx);
    }
}
