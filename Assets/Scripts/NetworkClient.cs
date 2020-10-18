using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using NetworkMessages;
using System;
using System.Text;

public class NetworkClient : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public string serverIP;
    public ushort serverPort;

    public GameObject playerGO;
    public string clientId { get; private set; }
    private List<NetworkPlayer> players;

    private void Awake()
    {
        players = new List<NetworkPlayer>();
    }

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP, serverPort);
        m_Connection = m_Driver.Connect(endpoint);
    }

    void SendToServer(string message)
    {
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message), Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    void OnConnect()
    {
        Debug.Log("We are now connected to the server");
    }

    void OnData(DataStreamReader stream)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch (header.cmd)
        {
            case Commands.NEW_CLIENT:
                NewClientMsg newClientMsg = JsonUtility.FromJson<NewClientMsg>(recMsg);
                SpawnPlayer(newClientMsg.player);
                Debug.Log("New client");
                break;
            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                UpdatePlayer(puMsg.updatedPlayers);
                //Debug.Log("Player updated");
                break;
            case Commands.SERVER_UPDATE:
                ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                Debug.Log("Server update message received!");
                break;
            case Commands.LIST_CLIENT:
                PlayerListMsg nplMsg = JsonUtility.FromJson<PlayerListMsg>(recMsg);
                SpawnPlayers(nplMsg.players);
                Debug.Log("List_Client");
                break;
            case Commands.MY_ID:
                NewClientMsg ncMsg = JsonUtility.FromJson<NewClientMsg>(recMsg);
                clientId = ncMsg.player.id;
                SpawnPlayer(ncMsg.player);
                break;
            case Commands.DROP_CLIENT:
                DropPlayerMsg dpMsg = JsonUtility.FromJson<DropPlayerMsg>(recMsg);
                DestroyPlayers(dpMsg.players);
                Debug.Log("Drop players");
                break;
            default:
                Debug.Log("Unrecognized message received!");
                break;
        }
    }
    private void DestroyPlayers(NetworkObjects.NetworkPlayer[] players)
    {
        for (int i = 0; i < players.Length; i++)
        {
            for (int j = 0; j < this.players.Count; j++)
            {
                if (players[i].id == this.players[j].netId)
                {
                    Destroy(this.players[j].gameObject);
                    this.players.RemoveAt(j);
                    --j;
                }
            }
        }
    }

    private void UpdatePlayer(NetworkObjects.NetworkPlayer[] players)
    {
        for (int i = 0; i < players.Length; i++)
        {
            for (int j = 0; j < this.players.Count; j++)
            {
                if (this.players[j].netId == players[i].id)
                {
                    this.players[j].transform.position = players[i].position;
                    this.players[j].transform.rotation = players[i].rotation;
                }
            }

        }
    }

    private void SpawnPlayer(NetworkObjects.NetworkPlayer player)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].netId == player.id)
            {
                return;
            }
        }

        NetworkPlayer temp = Instantiate(playerGO, player.position, player.rotation).GetComponent<NetworkPlayer>();
        temp.SetPlayer(player.id, player.position, player.rotation.eulerAngles, player.color);

        players.Add(temp);
    }

    private void SpawnPlayers(NetworkObjects.NetworkPlayer[] players)
    {
        for (int i = 0; i < players.Length; i++)
        {
            SpawnPlayer(players[i]);
        }
    }

    public void UpdatePlayer(string id, Vector3 position, Vector3 rotation)
    {
        ServerUpdateMsg msg = new ServerUpdateMsg();
        msg.player.id = id;
        msg.player.position = position;
        msg.player.rotation.eulerAngles = rotation;
        msg.player.lastBeat = System.DateTime.Now;
        SendToServer(JsonUtility.ToJson(msg));
    }

    void Disconnect()
    {
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);
    }

    void OnDisconnect()
    {
        Debug.Log("Client got disconnected from server");
        m_Connection = default(NetworkConnection);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }

    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        cmd = m_Connection.PopEvent(m_Driver, out stream);
        while (cmd != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                OnDisconnect();
            }

            cmd = m_Connection.PopEvent(m_Driver, out stream);
        }
    }
}
