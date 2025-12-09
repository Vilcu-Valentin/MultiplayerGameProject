using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera _virtualCamera;
    [SerializeField] private GameObject infoText;
    private CinemachinePOV cPov;
    [HideInInspector] public bool IsMovingCamera { get; private set; } = false;

    private void Start()
    {
        cPov = _virtualCamera.GetCinemachineComponent<CinemachinePOV>();
        cPov.m_HorizontalAxis.m_MaxSpeed = 0;
        cPov.m_VerticalAxis.m_MaxSpeed = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Cursor.visible = false;

            IsMovingCamera = true;
            infoText.SetActive(false);

            cPov.m_HorizontalAxis.m_MaxSpeed = 300;
            cPov.m_VerticalAxis.m_MaxSpeed = 300;
        }

        if (Input.GetMouseButtonUp(1))
        {
            Cursor.visible = true;
            IsMovingCamera = false;
            cPov.m_HorizontalAxis.m_MaxSpeed = 0;
            cPov.m_VerticalAxis.m_MaxSpeed = 0;
        }
    }
}
