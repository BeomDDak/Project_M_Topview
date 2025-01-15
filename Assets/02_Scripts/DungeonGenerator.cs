using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;

public enum RoomType
{
    Start,
    Normal,
    Vamsur,
    Boss,
}

[System.Serializable]
public class RoomPrefabs
{
    public GameObject normalRoom;
    public GameObject startRoom;
    public GameObject bossRoom;
    public GameObject vamsurRoom;
    public GameObject doorPrefab;
}

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private RoomPrefabs roomPrefabs;
    [SerializeField] private GameObject playerPrefab;  // �÷��̾� ������
    [SerializeField] private CinemachineVirtualCamera virtualCamera;  // �߰�

    [Header("������Ʈ ���� ����")]
    [SerializeField]
    [Range(0f, 3f)] private float playerSpawnY = 1f;
    [SerializeField]
    [Range(0f, 3f)] private float doorSpawnY = 1.5f;

    [Header("���� ���̾ƿ� ����")]
    [SerializeField] private int layoutSize = 3;    // 2x2, 3x3, 4x4 �� ����
    [SerializeField] private float roomSpacing = 10f;  // �� ������ ����

    [Header("Ư�� �� ����")]
    [SerializeField] private bool generateBossRoom = true;
    [SerializeField] private bool generateVamsur = true;
    [SerializeField][Range(0f, 1f)] private float vamsurSpawnChance = 0.2f;  // �켭�� ���� Ȯ��

    private class Room
    {
        public Vector2 position;
        public Vector2Int size;
        public RoomType type;
        public GameObject roomObject;
        public Dictionary<Vector2, Room> connections = new Dictionary<Vector2, Room>();
        public List<Door> doors = new List<Door>();

        public Room(Vector2 pos, Vector2Int size, RoomType type)
        {
            this.position = pos;
            this.size = size;
            this.type = type;
        }
    }

    private class Door
    {
        public Room room1;
        public Room room2;
        public Vector2 direction;
        public GameObject doorObject;

        public Door(Room r1, Room r2, Vector2 dir)
        {
            room1 = r1;
            room2 = r2;
            direction = dir;
        }
    }

    private List<Room> rooms = new List<Room>();
    private HashSet<Vector2> occupiedPositions = new HashSet<Vector2>();
    private List<Door> allDoors = new List<Door>();

    void Start()
    {
        GenerateDungeon();  // �� ����
        SpawnPlayer();      // �÷��̾� ����
    }

    void GenerateDungeon()
    {
        // �𼭸� ��ǥ 4�� ����
        Vector2[] corners = new Vector2[]
        {
            new Vector2(0, 0),                          // ���ϴ�
            new Vector2(0, layoutSize - 1),             // �»��
            new Vector2(layoutSize - 1, 0),             // ���ϴ�
            new Vector2(layoutSize - 1, layoutSize - 1)  // ����
        };

        // ������ �𼭸� �����Ͽ� ���۹� ��ġ
        Vector2 startPos = corners[Random.Range(0, corners.Length)];
        CreateRoom(startPos, new Vector2Int(1, 1), RoomType.Start);

        // NxN ���ڷ� �� ��ġ
        for (int x = 0; x < layoutSize; x++)
        {
            for (int y = 0; y < layoutSize; y++)
            {
                // ���۹� ��ġ�� �ǳʶٱ�
                if (x == startPos.x && y == startPos.y) continue;

                Vector2 position = new Vector2(x, y);

                // �������� ���۹��� �ݴ��� �𼭸��� ��ġ
                if (generateBossRoom && IsDiagonalCorner(position, startPos, layoutSize))
                {
                    CreateRoom(position, new Vector2Int(1, 1), RoomType.Boss);
                }
                else
                {
                    // �Ϲݹ�� �켭���� �����ϰ� ����
                    RoomType roomType = (generateVamsur && Random.value < vamsurSpawnChance) ?
                        RoomType.Vamsur : RoomType.Normal;
                    CreateRoom(position, new Vector2Int(1, 1), roomType);
                }
            }
        }

        GenerateMinimumConnections();
        GenerateAdditionalConnections();
        CreateDoorObjects();
    }

    void GenerateMinimumConnections()
    {
        Dictionary<Room, HashSet<Room>> sets = new Dictionary<Room, HashSet<Room>>();

        // �� ���� ������ �������� �ʱ�ȭ
        foreach (Room room in rooms)
        {
            sets[room] = new HashSet<Room> { room };
        }

        // ��� ������ ������ �����ϰ� ����
        List<(Room, Room, float)> possibleConnections = new List<(Room, Room, float)>();
        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                float distance = Vector2.Distance(rooms[i].position, rooms[j].position);
                possibleConnections.Add((rooms[i], rooms[j], distance));
            }
        }
        possibleConnections = possibleConnections.OrderBy(x => x.Item3).ToList();

        // �ּ� ���д� Ʈ�� ����
        foreach (var connection in possibleConnections)
        {
            Room room1 = connection.Item1;
            Room room2 = connection.Item2;

            if (sets[room1] != sets[room2])
            {
                Vector2 direction = (room2.position - room1.position).normalized;
                direction = NormalizeDirection(direction);

                CreateConnection(room1, room2, direction);

                var newSet = new HashSet<Room>(sets[room1].Union(sets[room2]));
                foreach (Room room in newSet)
                {
                    sets[room] = newSet;
                }
            }
        }
    }

    void GenerateAdditionalConnections()
    {
        foreach (Room room in rooms)
        {
            // ���۹��� �ǳʶٱ�
            if (room.type == RoomType.Start && room.connections.Count >= 1) continue;
            // �����浵 �ǳʶٱ�
            if (room.type == RoomType.Boss && room.connections.Count >= 1) continue;
            // �̹� 2�� �̻��� ������ �ִٸ� ��ŵ
            if (room.connections.Count >= 2) continue;

            var nearbyRooms = rooms
                .Where(r => r != room &&
                           Vector2.Distance(r.position, room.position) < 20 &&
                           // ���۹��̳� �������� �̹� ������ �ִٸ� ����
                           !((r.type == RoomType.Start && r.connections.Count >= 1) ||
                             (r.type == RoomType.Boss && r.connections.Count >= 1)))
                .OrderBy(r => Vector2.Distance(r.position, room.position))
                .ToList();

            foreach (Room nearbyRoom in nearbyRooms)
            {
                // ���۹��̳� �������� �̹� ������ �ִٸ� ��ŵ
                if ((nearbyRoom.type == RoomType.Start && nearbyRoom.connections.Count >= 1) ||
                    (nearbyRoom.type == RoomType.Boss && nearbyRoom.connections.Count >= 1))
                    continue;

                if (room.connections.Count < 2 && nearbyRoom.connections.Count < 2)
                {
                    Vector2 direction = (nearbyRoom.position - room.position).normalized;
                    direction = NormalizeDirection(direction);

                    if (!room.connections.ContainsKey(direction))
                    {
                        CreateConnection(room, nearbyRoom, direction);
                    }
                }
            }
        }
    }

    Vector2 NormalizeDirection(Vector2 direction)
    {
        return Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? new Vector2(Mathf.Sign(direction.x), 0)
            : new Vector2(0, Mathf.Sign(direction.y));
    }

    void CreateConnection(Room room1, Room room2, Vector2 direction)
    {
        room1.connections[direction] = room2;
        room2.connections[-direction] = room1;

        Door newDoor = new Door(room1, room2, direction);
        room1.doors.Add(newDoor);
        room2.doors.Add(newDoor);
        allDoors.Add(newDoor);
    }

    void CreateDoorObjects()
    {
        foreach (Door door in allDoors)
        {
            Vector3 doorPosition = CalculateDoorPosition(door) + Vector3.up * doorSpawnY;
            float rotation = CalculateDoorRotation(door.direction);
            door.doorObject = Instantiate(roomPrefabs.doorPrefab, doorPosition,
                Quaternion.Euler(0, rotation, 0));
        }
    }

    Vector3 CalculateDoorPosition(Door door)
    {
        Vector3 room1Pos = new Vector3(door.room1.position.x * roomSpacing, 0,
            door.room1.position.y * roomSpacing);
        Vector3 room2Pos = new Vector3(door.room2.position.x * roomSpacing, 0,
            door.room2.position.y * roomSpacing);
        return Vector3.Lerp(room1Pos, room2Pos, 0.5f);
    }

    float CalculateDoorRotation(Vector2 direction)
    {
        if (direction == Vector2.right) return 90;
        if (direction == Vector2.left) return 270;
        if (direction == Vector2.up) return 0;
        return 180;
    }

    void CreateRoom(Vector2 position, Vector2Int size, RoomType type)
    {
        Room newRoom = new Room(position, size, type);

        GameObject prefab = type switch
        {
            RoomType.Start => roomPrefabs.startRoom,
            RoomType.Boss => roomPrefabs.bossRoom,
            RoomType.Vamsur => roomPrefabs.vamsurRoom,
            _ => roomPrefabs.normalRoom
        };

        Vector3 worldPosition = new Vector3(position.x * roomSpacing, 0, position.y * roomSpacing);
        newRoom.roomObject = Instantiate(prefab, worldPosition, Quaternion.identity);
        rooms.Add(newRoom);
        occupiedPositions.Add(position);
    }

    bool IsDiagonalCorner(Vector2 pos, Vector2 startPos, int size)
    {
        return IsCorner(pos, size) && pos.x != startPos.x && pos.y != startPos.y;
    }

    bool IsCorner(Vector2 pos, int size)
    {
        return (pos.x == 0 || pos.x == size - 1) && (pos.y == 0 || pos.y == size - 1);
    }

    void SpawnPlayer()
    {
        // ���۹� ã��
        Room startRoom = rooms.Find(r => r.type == RoomType.Start);

        if (startRoom != null)
        {
            // �÷��̾� ����
            Vector3 spawnPosition = startRoom.roomObject.transform.position + Vector3.up * playerSpawnY;
            GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

            // ī�޶� Ÿ�� ����
            if (virtualCamera != null)
            {
                virtualCamera.Follow = player.transform;
                virtualCamera.LookAt = player.transform;
            }
        }
    }
}