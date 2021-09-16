using UnityEngine;
using UnityEditor;

public class OnlineProtocol 
{
    public enum Handler
    {
        LOBBY_GO,
        ONLINE_OBJECT_FIELDS,
        GAME_PROTOCOL_START, //user can use id from this
    }
}