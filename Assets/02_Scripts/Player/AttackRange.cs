using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AttackRange : MonoBehaviour
{
    [Header("���� ���� ����")]
    [Range(0, 30)]
    public float attackRadius;       // ����
    [Range(0, 360)]
    public float attackAngle;        // ����

    [Header("���̾� ����")]
    public LayerMask targetMask;
    public LayerMask obstacleMask;

    [Header("����ũ �� ����")]
    public Material attackRangeMaterial;
    public Color normalColor = new Color(0, 1, 0, 0.3f);
    public Color targetColor = new Color(1, 0, 0, 0.3f);

    // Ÿ�ٵ��� ��� ����Ʈ
    [HideInInspector]
    public List<Transform> attackableTargets = new List<Transform>();

    [Header("�޽� ����")]
    public MeshFilter attackRangeMeshFilter;

    void Start()
    {
        StartCoroutine(FindTargetsWithDelay(0.2f));
        UpdateRangeColor();
    }

    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindAttackableTargets();
        }
    }

    void FindAttackableTargets()
    {
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

        UpdateRangeColor();
    }

    void UpdateRangeColor()
    {
        if (attackRangeMaterial != null)
        {
            attackRangeMaterial.SetColor("_Color", attackableTargets.Count > 0 ? targetColor : normalColor);
        }
    }
}