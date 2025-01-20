using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FOVCameraEffect : MonoBehaviour
{
    public Material FOVMaterial;  // FOVMask 쉐이더를 사용하는 머티리얼
    public PlayerView playerFOV;  // 플레이어의 FOV 컴포넌트

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

        // FOV 마스크를 렌더링할 텍스처 생성
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
            // FOV 마스크 텍스처 업데이트
            UpdateFOVMaskTexture();

            // FOV 마스크를 쉐이더에 전달
            FOVMaterial.SetTexture("_FOVMask", fovMaskTexture);

            // 후처리 효과 적용
            Graphics.Blit(source, destination, FOVMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    void UpdateFOVMaskTexture()
    {
        // 현재 활성 렌더 텍스처를 저장
        RenderTexture currentRT = RenderTexture.active;

        // FOV 마스크 텍스처를 활성화
        RenderTexture.active = fovMaskTexture;

        // 텍스처 초기화
        GL.Clear(true, true, Color.black);

        // FOV 메시를 흰색으로 렌더링
        if (playerFOV.GetComponent<MeshFilter>().mesh != null)
        {
            Graphics.DrawMesh(
                playerFOV.GetComponent<MeshFilter>().mesh,
                playerFOV.transform.localToWorldMatrix,
                new Material(Shader.Find("Unlit/Color")) { color = Color.white },
                0
            );
        }

        // 이전 렌더 텍스처 복원
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