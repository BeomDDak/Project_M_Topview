using System;
using UnityEngine;

public static class GameEvents
{
    public static Action<GameObject> OnPlayerSpawned;

    // �̺�Ʈ �߻���ų �� null üũ�� ���� Ȯ�� �޼���
    public static void InvokeOnPlayerSpawned(GameObject player)
    {
        OnPlayerSpawned?.Invoke(player);
    }
}
