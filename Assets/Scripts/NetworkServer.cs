using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using Random = UnityEngine.Random;

public class NetworkServer : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public ushort serverPort;
    private NativeList<NetworkConnection> m_Connections;

    private List<NetworkObjects.NetworkPlayer> players;

    private void Awake()
    {
        players = new List<NetworkObjects.NetworkPlayer>();
    }

    void Start ()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = serverPort;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port " + serverPort);
        else
            m_Driver.Listen();

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

        InvokeRepeating("UpdatePlayersToClient", 1, 0.016f);
    }

    void SendToClient(string message, NetworkConnection c)
    {
        var writer = m_Driver.BeginSend(NetworkPipeline.Null, c);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }
    
    void OnConnect(NetworkConnection c)
    {
        // Set all player values when connecting
        NewClientMsg msg = new NewClientMsg();
        msg.player.id = c.InternalId.ToString();
        msg.player.position = new Vector3(0, 0, 0);
        msg.player.rotation.eulerAngles = new Vector3(0, 0, 0);
        msg.player.color.r = Random.Range(0.0f, 1.0f);
        msg.player.color.g = Random.Range(0.0f, 1.0f);
        msg.player.color.b = Random.Range(0.0f, 1.0f);
        msg.player.lastBeat = DateTime.Now;

        PlayerListMsg list = new PlayerListMsg(players);
        foreach (NetworkConnection connection in m_Connections)
        {
            SendToClient(JsonUtility.ToJson(msg), connection);
        }

        msg.cmd = Commands.MY_ID;
        SendToClient(JsonUtility.ToJson(msg), c);
        SendToClient(JsonUtility.ToJson(list), c);
        players.Add(msg.player);
        m_Connections.Add(c);
        Debug.Log("Accepted a connection");
    }

    void OnData(DataStreamReader stream, int i)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch(header.cmd)
        {
            case Commands.NEW_CLIENT:
            NewClientMsg hsMsg = JsonUtility.FromJson<NewClientMsg>(recMsg);
            Debug.Log("NEW_CLIENT message received!");
            break;
            case Commands.PLAYER_UPDATE:
            PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
            Debug.Log("Player update message received!");
            break;
            case Commands.SERVER_UPDATE:
            ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
            UpdatePlayersFromSever(suMsg);
            Debug.Log("Server update message received!");
            break;
            default:
            Debug.Log("SERVER ERROR: Unrecognized message received!");
            break;
        }
    }

    void UpdatePlayersFromSever(ServerUpdateMsg serverMsg)
    {
        for (int j = 0; j < players.Count; j++)
        {
            if (players[j].id == serverMsg.player.id)
            {
                players[j].position = serverMsg.player.position;
                players[j].rotation = serverMsg.player.rotation;
                players[j].lastBeat = serverMsg.player.lastBeat;
            }
        }
    }

    void OnDisconnect(int i)
    {
        Debug.Log("Client disconnected from server");
        m_Connections[i] = default(NetworkConnection);
    }

    void UpdatePlayersToClient()
    {
        PlayerUpdateMsg updatedPlayerMsg = new PlayerUpdateMsg(players);

        foreach(NetworkConnection c in m_Connections)
        {
           SendToClient(JsonUtility.ToJson(updatedPlayerMsg), c);
        }
    }

    // Remove players that have disconnected
    void CleanClients()
    {
        List<NetworkObjects.NetworkPlayer> droppedPlayers = new List<NetworkObjects.NetworkPlayer>();
        
        for(int k = 0; k < players.Count; k++)
        {
            if ((System.DateTime.Now - players[k].lastBeat).TotalSeconds > 5.0f)
            {
                droppedPlayers.Add(players[k]);
                players.RemoveAt(k);
                --k;
            }
        }

        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                for(int j = 0; j < players.Count; j++)
                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        if(droppedPlayers.Count > 0)
        {
            DropPlayerMsg dpMsg = new DropPlayerMsg(droppedPlayers);
            for(int i = 0; i < m_Connections.Length; i++)
            {
                SendToClient(JsonUtility.ToJson(dpMsg), m_Connections[i]);
            }
        }
    }

    void Update ()
    {
        m_Driver.ScheduleUpdate().Complete();

        // CleanUpConnections
        CleanClients();

        // AcceptNewConnections
        NetworkConnection c = m_Driver.Accept();

        while (c  != default(NetworkConnection))
        {            
            OnConnect(c);

            // Check if there is another new connection
            c = m_Driver.Accept();
        }


        // Read Incoming Messages
        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            Assert.IsTrue(m_Connections[i].IsCreated);
            
            NetworkEvent.Type cmd;
            cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            while (cmd != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    OnData(stream, i);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    OnDisconnect(i);
                }

                cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            }
        }
    }
}