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
    [SerializeField] private GameObject playerPrefab;  // 플레이어 프리팹
    [SerializeField] private CinemachineVirtualCamera virtualCamera;  // 추가

    [Header("오브젝트 높이 설정")]
    [SerializeField]
    [Range(0f, 3f)] private float playerSpawnY = 1f;
    [SerializeField]
    [Range(0f, 3f)] private float doorSpawnY = 1.5f;

    [Header("던전 레이아웃 설정")]
    [SerializeField] private int layoutSize = 3;    // 2x2, 3x3, 4x4 중 선택
    [SerializeField] private float roomSpacing = 10f;  // 방 사이의 간격

    [Header("특수 방 설정")]
    [SerializeField] private bool generateBossRoom = true;
    [SerializeField] private bool generateVamsur = true;
    [SerializeField][Range(0f, 1f)] private float vamsurSpawnChance = 0.2f;  // 뱀서방 등장 확률

    #region Room
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
    #endregion

    #region Door
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
    #endregion

    private List<Room> rooms = new List<Room>();        // 생성된 방들을 담고 있는 리스트
    private HashSet<Vector2> occupiedPositions = new HashSet<Vector2>();    // 방이 생성될 때 중복확인하는 해시셋
    private List<Door> allDoors = new List<Door>();     // 생성된 문들을 담고 있는 리스트

    void Start()
    {
        GenerateDungeon();  // 맵 생성
        SpawnPlayer();      // 플레이어 생성
    }

    // 던전 생성 메인 메서드
    void GenerateDungeon()
    {
        // 모서리 좌표 4개 설정
        Vector2[] corners = new Vector2[]
        {
            new Vector2(0, 0),                           // 좌하단
            new Vector2(0, layoutSize - 1),              // 좌상단
            new Vector2(layoutSize - 1, 0),              // 우하단
            new Vector2(layoutSize - 1, layoutSize - 1)  // 우상단
        };

        // 랜덤한 모서리 선택하여 시작방 배치
        Vector2 startPos = corners[Random.Range(0, corners.Length)];
        CreateRoom(startPos, new Vector2Int(1, 1), RoomType.Start);

        // NxN 격자로 방 배치
        // x,y 는 레이아웃 배치 시 지정되는 값
        for (int x = 0; x < layoutSize; x++)
        {
            for (int y = 0; y < layoutSize; y++)
            {
                // 시작방 위치는 건너뛰기
                if (x == startPos.x && y == startPos.y)
                    continue;

                Vector2 position = new Vector2(x, y);

                // 보스방은 시작방의 대각선으로 반대편 모서리에 배치
                if (generateBossRoom && IsDiagonalCorner(position, startPos, layoutSize))
                {
                    CreateRoom(position, new Vector2Int(1, 1), RoomType.Boss);
                }
                else
                {
                    // 일반방과 뱀서방을 랜덤하게 생성
                    RoomType roomType = (generateVamsur && Random.value < vamsurSpawnChance) ? RoomType.Vamsur : RoomType.Normal;
                    CreateRoom(position, new Vector2Int(1, 1), roomType);
                }
            }
        }

        GenerateMinimumConnections();
        GenerateAdditionalConnections();
        CreateDoorObjects();
    }

    // 크루스칼 알고리즘 ( 모든 방에 도달할 수 있도록 함 )
    void GenerateMinimumConnections()
    {
        Dictionary<Room, HashSet<Room>> sets = new Dictionary<Room, HashSet<Room>>();

        // 각 방을 별도의 집합으로 초기화
        foreach (Room room in rooms)
        {
            sets[room] = new HashSet<Room> { room };
        }

        // 모든 가능한 연결을 생성하고 정렬
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

        // 최소 스패닝 트리 생성
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

    // 추가적인 연결 생성 ( 보스방과 시작방은 연결 1개 나머지 방은 2개까지 연결 가능하도록 만들려고는 함 )
    void GenerateAdditionalConnections()
    {
        foreach (Room room in rooms)
        {
            // 시작방은 건너뛰기
            if (room.type == RoomType.Start && room.connections.Count >= 1) continue;
            // 보스방도 건너뛰기
            if (room.type == RoomType.Boss && room.connections.Count >= 1) continue;
            // 이미 2개 이상의 연결이 있다면 스킵
            if (room.connections.Count >= 2) continue;

            var nearbyRooms = rooms
                .Where(r => r != room &&
                           Vector2.Distance(r.position, room.position) < 20 &&
                           // 시작방이나 보스방이 이미 연결이 있다면 제외
                           !((r.type == RoomType.Start && r.connections.Count >= 1) ||
                             (r.type == RoomType.Boss && r.connections.Count >= 1)))
                .OrderBy(r => Vector2.Distance(r.position, room.position))
                .ToList();

            foreach (Room nearbyRoom in nearbyRooms)
            {
                // 시작방이나 보스방이 이미 연결이 있다면 스킵
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

    // 방향 계산
    Vector2 NormalizeDirection(Vector2 direction)
    {
        return Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? new Vector2(Mathf.Sign(direction.x), 0)
            : new Vector2(0, Mathf.Sign(direction.y));
    }

    // 방들의 연결
    void CreateConnection(Room room1, Room room2, Vector2 direction)
    {
        room1.connections[direction] = room2;
        room2.connections[-direction] = room1;

        Door newDoor = new Door(room1, room2, direction);
        room1.doors.Add(newDoor);
        room2.doors.Add(newDoor);
        allDoors.Add(newDoor);
    }

    // 방과 방 사이에 문 오브젝트 생성
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

    // 문 위치 계산
    Vector3 CalculateDoorPosition(Door door)
    {
        Vector3 room1Pos = new Vector3(door.room1.position.x * roomSpacing, 0,
            door.room1.position.y * roomSpacing);
        Vector3 room2Pos = new Vector3(door.room2.position.x * roomSpacing, 0,
            door.room2.position.y * roomSpacing);
        return Vector3.Lerp(room1Pos, room2Pos, 0.5f);
    }

    // 문 회전 계산
    float CalculateDoorRotation(Vector2 direction)
    {
        if (direction == Vector2.right) return 90;
        if (direction == Vector2.left) return 270;
        if (direction == Vector2.up) return 0;
        return 180;
    }

    // 방 생성
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

    // 대각선 인지 확인
    bool IsDiagonalCorner(Vector2 pos, Vector2 startPos, int size)
    {
        return IsCorner(pos, size) && pos.x != startPos.x && pos.y != startPos.y;
    }

    // 모서리 인지 확인
    bool IsCorner(Vector2 pos, int size)
    {
        return (pos.x == 0 || pos.x == size - 1) && (pos.y == 0 || pos.y == size - 1);
    }

    // 플레이어 생성 및 카메라 타겟 설정
    void SpawnPlayer()
    {
        // 시작방 찾기
        Room startRoom = rooms.Find(r => r.type == RoomType.Start);

        if (startRoom != null)
        {
            // 플레이어 생성
            Vector3 spawnPosition = startRoom.roomObject.transform.position + Vector3.up * playerSpawnY;
            GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

            // 카메라 타겟 설정
            if (virtualCamera != null)
            {
                virtualCamera.Follow = player.transform;
                virtualCamera.LookAt = player.transform;
            }

            // 플레이어 생성 이벤트 발생
            GameEvents.InvokeOnPlayerSpawned(player);
        }
    }
}