using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulbIndicator : MonoBehaviour
{
    private Renderer m_Renderer;

    [SerializeField] private Material litMaterial;
    [SerializeField] private Material unlitMaterial;
    // Start is called before the first frame update
    private void Awake()
    {
        m_Renderer = GetComponent<Renderer>();
    }

    public void TurnOn()
    {
        m_Renderer.material = litMaterial;
    }

    public void TurnOff()
    {
        m_Renderer.material = unlitMaterial;
    }
}
