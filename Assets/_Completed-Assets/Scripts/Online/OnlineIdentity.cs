using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnlineIdentity : MonoBehaviour
{

    public ulong m_uid = 0;
    public uint m_localPlayerAuthority = 0; //host by default

    public bool HasAuthority()
    {
        return (OnlineManager.Instance.IsHost()?0:1) == m_localPlayerAuthority;
    }

}
