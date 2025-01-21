using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    private float moveSpeed = 6f;
    private float rotationSpeed = 720f; // �ʴ� ȸ�� �ӵ� (�� ����)

    void Update()
    {
        InputKey();
    }

    void InputKey()
    {
        // Ű ���� ����
        Vector3 moveDir = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            moveDir += Vector3.forward;
        }

        if (Input.GetKey(KeyCode.S))
        {
            moveDir += Vector3.back;
        }

        if (Input.GetKey(KeyCode.A))
        {
            moveDir += Vector3.left;
        }

        if (Input.GetKey(KeyCode.D))
        {
            moveDir += Vector3.right;
        }

        // �̵��� ȸ��
        if (moveDir != Vector3.zero)
        {
            // �̵� ���� ����ȭ
            moveDir.Normalize();

            // ĳ���� �̵�
            transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);

            // �̵� ������ �ٶ󺸴� ȸ���� ���
            Quaternion toRotation = Quaternion.LookRotation(moveDir, Vector3.up);

            // �ε巯�� ȸ�� ����
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                toRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}
