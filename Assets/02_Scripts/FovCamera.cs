using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FOVCameraEffect : MonoBehaviour
{
    public Material FOVMaterial;  // FOVMask ���̴��� ����ϴ� ��Ƽ����
    public PlayerView playerFOV;  // �÷��̾��� FOV ������Ʈ

    private Camera cam;
    private RenderTexture fovMaskTexture;

    void OnEnable()
    {
        GameEvents.OnPlayerSpawned += OnPlayerSpawned;
    }

    void OnDisable()
    {
        GameEvents.OnPlayerSpawned -= OnPlayerSpawned;
    }

    void Start()
    {
        cam = GetComponent<Camera>();

        // FOV ����ũ�� �������� �ؽ�ó ����
        fovMaskTexture = new RenderTexture(Screen.width, Screen.height, 0);
        fovMaskTexture.Create();
    }

    private void OnPlayerSpawned(GameObject player)
    {
        playerFOV = player.GetComponent<PlayerView>();
        if (playerFOV == null)
        {
            playerFOV = player.AddComponent<PlayerView>();
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (FOVMaterial != null && playerFOV != null)
        {
            // FOV ����ũ �ؽ�ó ������Ʈ
            UpdateFOVMaskTexture();

            // FOV ����ũ�� ���̴��� ����
            FOVMaterial.SetTexture("_FOVMask", fovMaskTexture);

            // ��ó�� ȿ�� ����
            Graphics.Blit(source, destination, FOVMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    void UpdateFOVMaskTexture()
    {
        // ���� Ȱ�� ���� �ؽ�ó�� ����
        RenderTexture currentRT = RenderTexture.active;

        // FOV ����ũ �ؽ�ó�� Ȱ��ȭ
        RenderTexture.active = fovMaskTexture;

        // �ؽ�ó �ʱ�ȭ
        GL.Clear(true, true, Color.black);

        // FOV �޽ø� ������� ������
        if (playerFOV.GetComponent<MeshFilter>().mesh != null)
        {
            Graphics.DrawMesh(
                playerFOV.GetComponent<MeshFilter>().mesh,
                playerFOV.transform.localToWorldMatrix,
                new Material(Shader.Find("Unlit/Color")) { color = Color.white },
                0
            );
        }

        // ���� ���� �ؽ�ó ����
        RenderTexture.active = currentRT;
    }

    void OnDestroy()
    {
        if (fovMaskTexture != null)
        {
            fovMaskTexture.Release();
            Destroy(fovMaskTexture);
        }
    }
}