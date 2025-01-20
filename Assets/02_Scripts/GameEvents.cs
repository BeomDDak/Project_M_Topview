using System;
using UnityEngine;

public static class GameEvents
{
    public static Action<GameObject> OnPlayerSpawned;

    // 이벤트 발생시킬 때 null 체크를 위한 확장 메서드
    public static void InvokeOnPlayerSpawned(GameObject player)
    {
        OnPlayerSpawned?.Invoke(player);
    }
}
