using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(OnlineIdentity), true)]
public class OnlineIdentityEditor : Editor
{
    int selectedField = 0;
    bool openreflection = true;
    public override void OnInspectorGUI()
    {
        var id = target as OnlineIdentity;
        id.m_type = (OnlineIdentity.Type)EditorGUILayout.EnumPopup("Type", id.m_type);

        GUI.enabled = false;
        switch(id.m_type)
        {
            case OnlineIdentity.Type.Determinist:
                EditorGUILayout.LabelField("Should be used for parallel instantiation through code.\nYou must set m_uid in script manually (use OnlineObject.ComputeDeterministId to easily set one without colliding with auto generated ones) ", EditorStyles.textArea);
                GUI.enabled = true;
                break;
            case OnlineIdentity.Type.Dynamic:
                EditorGUILayout.LabelField("Should be used for dynamic instanciation\nDynamic object must be registered into OnlineObjectManager (editor) to properly works and spawned using OnlineObject.Instantiate on Host", EditorStyles.textArea);
                break;
            case OnlineIdentity.Type.Static:
                EditorGUILayout.LabelField("Should be used for object in scene\nObject will be unactivated on load, then activated by Host", EditorStyles.textArea);
                break;
            case OnlineIdentity.Type.HostOnly:
                EditorGUILayout.LabelField("Should be used for object in scene that should exist only for Host", EditorStyles.textArea);
                break;
        }
        EditorGUILayout.LongField("UID", (long)id.m_uid);
        GUI.enabled = true;
    }
}
#endif

/// <summary>
/// This component add online existency to a game object
/// As soon as you set it, your object WON'T BE SPAWNED on client anymore (or won't be active)
/// instead Host will handle the spawn
/// 
/// Each Online Object should have an Unique ID so different client can refer to the 'same' objet with it
/// This unique ID could be set :
///     -automatically if you decide to spawn your object Statically or Dynamically (check OnlineObjectManager)
///     -manually if you decide to give it a determinist one (you must use OnlineObject.ComputeDeterministID()) 
/// </summary>
public class OnlineIdentity : MonoBehaviour
{

    public ulong m_uid = 0;
    public uint m_localPlayerAuthority = 0; //host by default
    public string m_srcName;

    public enum Type
    {
        Static, //object in scene, sync between host and clients
        Dynamic, //object dynamically spawned by script using OnlineObject.Instanciate
        Determinist, //object dynamically spawned by script but parallel on each clients using GameObject.Instanciate, you need to give a determinist ids
        HostOnly //object existing only on Host
    };
    public Type m_type = Type.Static;

    // Start is called before the first frame update
    void Awake()
    {
        switch (m_type)
        {
            case Type.HostOnly:
                {
                    break;
                }
            case Type.Dynamic:
                {
                    break;
                }
            case Type.Static:
                {
                    m_srcName = transform.name;
                    OnlineObjectManager.Instance.RegisterStaticObject(gameObject);
                    break;
                }
            case Type.Determinist:
                {
                    //check if uid is correctly setted and is determinist
                    break;
                }
        }

    }
    void Start()
    {
        switch (m_type)
        {
            case Type.HostOnly:
                {
                    if (!OnlineManager.Instance.IsHost())
                    {
                        Destroy(gameObject);
                        return;
                    }
                    break;
                }
            case Type.Dynamic:
                {
                    if (m_uid == 0)
                    {
                        Destroy(gameObject);
                        return;
                    }
                    break;
                }
            case Type.Static:
                {
                    if (OnlineManager.Instance.IsHost())
                    {
                        OnlineObjectManager.Instance.Spawn(gameObject);
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }
                    break;
                }
            case Type.Determinist:
                {
                    //check if uid is correctly setted and is determinist
                    break;
                }
        }

    }


    public bool HasAuthority()
    {
        switch (m_type)
        {
            case Type.HostOnly: return OnlineManager.Instance.IsHost();
            case Type.Static:
            case Type.Dynamic:
            case Type.Determinist:
                {
                    return m_localPlayerAuthority == OnlinePlayerManager.Instance.m_localPlayerID;
                }
        }
        return false;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDestroy()
    {
        switch (m_type)
        {
            case Type.HostOnly:
                {
                    break;
                }
            case Type.Dynamic:
                {
                    if (m_uid != 0)
                    {
                        OnlineObjectManager.Instance.Despawn(gameObject);
                        return;
                    }
                    break;
                }
            case Type.Static:
                {
                    break;
                }
            case Type.Determinist:
                {
                    break;
                }
        }
    }



}
