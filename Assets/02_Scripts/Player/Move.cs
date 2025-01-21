using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    private float moveSpeed = 6f;
    private float rotationSpeed = 720f; // 초당 회전 속도 (도 단위)

    void Update()
    {
        InputKey();
    }

    void InputKey()
    {
        // 키 방향 설정
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

        // 이동과 회전
        if (moveDir != Vector3.zero)
        {
            // 이동 방향 정규화
            moveDir.Normalize();

            // 캐릭터 이동
            transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);

            // 이동 방향을 바라보는 회전값 계산
            Quaternion toRotation = Quaternion.LookRotation(moveDir, Vector3.up);

            // 부드러운 회전 적용
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                toRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}
