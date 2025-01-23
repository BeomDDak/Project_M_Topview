using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FieldOfView : MonoBehaviour 
{
    [Header("시야 설정")]
    [Range(0, 10)]
    public float viewRadius;
	[Range(0, 360)]
	public float viewAngle;

    [Header("공격 범위 설정")]
    [Range(0, 30)]
    public float attackRadius;       // 범위
    [Range(0, 360)]
    public float attackAngle;        // 각도

    [Header("레이어 설정")]
    public LayerMask targetMask;
	public LayerMask obstacleMask;

    [Header("공격범위 색 변경")]
    public Material attackRangeMaterial;
    public Color normalColor = new Color(0, 1, 0, 0.3f);
    public Color targetColor = new Color(1, 0, 0, 0.3f);

    // 타겟들이 담길 리스트
    [HideInInspector]
	public List<Transform> visibleTargets = new List<Transform>();
    [HideInInspector]
    public List<Transform> attackableTargets = new List<Transform>();

    public float meshResolution;
	public int edgeResolveIterations;
	public float edgeDstThreshold;

    [Header("메시 필터")]
    public MeshFilter viewMeshFilter;
    public MeshFilter attackRangeMeshFilter;
    Mesh viewMesh;
    Mesh attackMesh;

    void Start()
	{
        // 시야 메시 초기화
        viewMesh = new Mesh();
		viewMesh.name = "View Mesh";
		viewMeshFilter.mesh = viewMesh;

        // 공격범위 메시 초기화
        attackMesh = new Mesh();
        attackMesh.name = "Attack Mesh";
        attackRangeMeshFilter.mesh = attackMesh;

        StartCoroutine("FindTargetsWithDelay", 0.2f);
        UpdateRangeColor();

    }

    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindTargets();
        }
    }

    void LateUpdate()
    {
        DrawFieldOfView(viewMesh, viewRadius, viewAngle);
        DrawFieldOfView(attackMesh, attackRadius, attackAngle);
    }

    void FindTargets()
    {
        // 시야 내 타겟 찾기
        visibleTargets.Clear();
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);
        foreach (Collider col in targetsInViewRadius)
        {
            Transform target = col.transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            if (!Physics.Raycast(transform.position, dirToTarget, Vector3.Distance(transform.position, target.position), obstacleMask))
            {
                visibleTargets.Add(target);
            }
        }

        // 공격 범위 내 타겟 찾기
        attackableTargets.Clear();
        Collider[] targetsInRadius = Physics.OverlapSphere(transform.position, attackRadius, targetMask);

        for (int i = 0; i < targetsInRadius.Length; i++)
        {
            Transform target = targetsInRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, dirToTarget) < attackAngle / 2)
            {
                float distToTarget = Vector3.Distance(transform.position, target.position);
                if (!Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask))
                {
                    attackableTargets.Add(target);
                }
            }
        }

        UpdateRangeColor(); // 현재 업데이트에서 체크 이벤트로 바꾸면 좋을듯
    }

    void DrawFieldOfView(Mesh mesh, float radius, float angle)
    {
        int stepCount = Mathf.RoundToInt(angle * meshResolution);
        float stepAngleSize = angle / stepCount;
        List<Vector3> viewPoints = new List<Vector3>();

        for (int i = 0; i <= stepCount; i++)
        {
            float currentAngle = transform.eulerAngles.y - angle / 2 + stepAngleSize * i;
            ViewCastInfo newViewCast = ViewCast(currentAngle, radius);
            viewPoints.Add(newViewCast.point);
        }

        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    ViewCastInfo ViewCast(float globalAngle, float radius)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit hit;

        if (Physics.Raycast(transform.position, dir, out hit, radius, obstacleMask))
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        else
        {
            return new ViewCastInfo(false, transform.position + dir * radius, radius, globalAngle);
        }
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;

        public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle)
        {
            hit = _hit;
            point = _point;
            dst = _dst;
            angle = _angle;
        }
    }

    void UpdateRangeColor()
    {
        if (attackRangeMaterial != null)
        {
            attackRangeMaterial.SetColor("_Color", attackableTargets.Count > 0 ? targetColor : normalColor);
        }
    }
}
