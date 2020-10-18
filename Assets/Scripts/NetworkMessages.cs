using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkMessages
{
    public enum Commands
    {
        NEW_CLIENT,
        LIST_CLIENT,
        PLAYER_UPDATE,
        SERVER_UPDATE,
        MY_ID,
        DROP_CLIENT,
        HeartBeat,
    }

    [System.Serializable]
    public class NetworkHeader
    {
        public Commands cmd;
    }

    [System.Serializable]
    public class NewClientMsg:NetworkHeader
    {
        public NetworkObjects.NetworkPlayer player;

        public NewClientMsg(){ 
            cmd = Commands.NEW_CLIENT;
            player = new NetworkObjects.NetworkPlayer();
        }
    }

    [System.Serializable]
    public class PlayerUpdateMsg:NetworkHeader
    {
        public NetworkObjects.NetworkPlayer[] updatedPlayers;

        public PlayerUpdateMsg()
        {
            cmd = Commands.PLAYER_UPDATE;
        }

        public PlayerUpdateMsg(List<NetworkObjects.NetworkPlayer> players)
        {
            cmd = Commands.PLAYER_UPDATE;
            updatedPlayers = new NetworkObjects.NetworkPlayer[players.Count];
            for (int i = 0; i < players.Count; i++)
            {
                updatedPlayers[i] = players[i];
            }
        }
    };

    [System.Serializable]
    public class  ServerUpdateMsg:NetworkHeader
    {
        public NetworkObjects.NetworkPlayer player;

        public ServerUpdateMsg()
        {
            player = new NetworkObjects.NetworkPlayer();
            cmd = Commands.SERVER_UPDATE;
        }
    }

    [System.Serializable]
    public class PlayerListMsg: NetworkHeader
    {
        public NetworkObjects.NetworkPlayer[] players;

        public PlayerListMsg()
        {
            cmd = Commands.LIST_CLIENT;
        }

        public PlayerListMsg(List<NetworkObjects.NetworkPlayer> players)
        {
            cmd = Commands.LIST_CLIENT;
            this.players = new NetworkObjects.NetworkPlayer[players.Count];
            for(int i = 0; i < players.Count; i++)
            {
                this.players[i] = players[i];
            }
        }
    }

    [System.Serializable]
    public class DropPlayerMsg : NetworkHeader
    {
        public NetworkObjects.NetworkPlayer[] players;

        public DropPlayerMsg(List<NetworkObjects.NetworkPlayer> players)
        {
            cmd = Commands.DROP_CLIENT;
            this.players = new NetworkObjects.NetworkPlayer[players.Count];
            for (int i = 0; i < players.Count; i++)
            {
                this.players[i] = players[i];
            }
        }
    }
} 

namespace NetworkObjects
{
    [System.Serializable]
    public class NetworkObject
    {
        public string id;
    }

    [System.Serializable]
    public class NetworkPlayer : NetworkObject
    {
        public System.DateTime lastBeat;
        public Color color;
        public Vector3 position;
        public Quaternion rotation;

        public NetworkPlayer() 
        {
            lastBeat = System.DateTime.Now;
            color = new Color(0,0,0);
            position = new Vector3(0,0,0);
            rotation.eulerAngles = new Vector3(0,0,0);
        }
    }
}
