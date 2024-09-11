using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimacioEines : MonoBehaviour
{
    readonly float toDegrees = 180 / Mathf.PI;

    public InputActionProperty toolAnimation;

    [HideInInspector]
    public float triggerValue;

    GameObject tub;

    GameObject partSuperior;
    GameObject pivotSuperior;

    GameObject partInferior;
    GameObject pivotInferior;

    GameObject puntFix;
    GameObject pivot;
    GameObject fix;

    Vector3 e1;
    Vector3 e1l;
    GameObject e2;

    float alpha0;
    float beta0;

    float llargadaD1;
    float llargadaD2;

    GrabbingSoftbody grabbingSoftbody;

    Vector3 newPos;
    float diferencia;

    void InicialtizaObjectes()
    {
        GameObject capEina = transform.Find(transform.name).gameObject;
        tub = capEina.transform.Find("Vara").gameObject;

        partSuperior = tub.transform.Find("Part Superior").gameObject;
        pivotSuperior = partSuperior.transform.Find("Pivot").gameObject;

        partInferior = tub.transform.Find("Part Inferior").gameObject;
        pivotInferior = partInferior.transform.Find("Pivot").gameObject;

        puntFix = tub.transform.Find("Punt Fix").gameObject;
        pivot = tub.transform.Find("PIVOT").gameObject;
        fix = capEina.transform.Find("Fix").gameObject;
    }

    void Awake()
    {
        InicialtizaObjectes();

        e1 = partSuperior.transform.position;
        e1l = partSuperior.transform.localPosition;

        Vector3 d1 = (pivot.transform.position - e1) / transform.localScale.x;
        llargadaD1 = Vector3.Magnitude(d1);

        float d1y0 = Math.Abs(pivot.transform.localPosition.y - partSuperior.transform.localPosition.y);

        alpha0 = Mathf.Acos(d1y0 / llargadaD1) * toDegrees;

        Vector3 d2 = (puntFix.transform.position - pivot.transform.position) / transform.localScale.x;
        llargadaD2 = Vector3.Magnitude(d2);

        float d2x0 = Math.Abs(puntFix.transform.localPosition.x - pivot.transform.localPosition.x);

        beta0 = Mathf.Acos(d2x0 / llargadaD2) * toDegrees;

        e2 = new GameObject();
        e2.name = "e2";
        e2.transform.parent = tub.transform;

        e2.transform.localPosition = new Vector3(e1l.x, puntFix.transform.localPosition.y + Mathf.Sqrt(Mathf.Pow(llargadaD2,2)-Mathf.Pow(llargadaD1,2)), e1l.z);

        diferencia = e2.transform.localPosition.y -partSuperior.transform.localPosition.y;
        newPos = tub.transform.localPosition;
    }

    private void Start()
    {
        grabbingSoftbody = GetComponent<GrabbingSoftbody>();
    }

    // Update is called once per frame
    void Update()
    {
        triggerValue = toolAnimation.action.ReadValue<float>();
        if(grabbingSoftbody != null )
        {
            if (grabbingSoftbody.elementsEnganxats.Count == 0)
            {
                CalculaPosicioEines(1 - triggerValue);
            }
            else if (triggerValue < 0.8)
            {
                CalculaPosicioEines(1 - triggerValue);
            }
        }
        else
        {
            CalculaPosicioEines(1 - triggerValue);
        }
    }

    void CalculaPosicioEines(float triggerValue)
    {
        Vector3 newPos1 = new(newPos.x, newPos.y + diferencia * triggerValue, newPos.z);

        tub.transform.localPosition = newPos1;

        float A = Math.Abs((newPos1.y + partSuperior.transform.localPosition.y) - fix.transform.localPosition.y);
        float D1 = Mathf.Pow(llargadaD1, 2);
        float D2 = Mathf.Pow(llargadaD2, 2);

        float dx_1 = Mathf.Sqrt(2*D1*(A*A+D2)-Mathf.Pow(A*A-D2,2)-D1*D1) / (2*A);
        //float dx_2 = dx_1;
        float dy_1 = (A*A+D1-D2) / (2*A);
        //float dy_2 = (A * A - D1 + D2) / (2 * A);

        float alpha = Mathf.Acos(Math.Abs(dy_1) / llargadaD1) * toDegrees;
        Vector3 nouAngleAlpha = new(0, 0, alpha - alpha0);

        float beta = Mathf.Acos(Mathf.Abs(dx_1) / llargadaD2) * toDegrees;
        Vector3 nouAngleBeta = new(0, 0, (beta0 - beta) + nouAngleAlpha.z);


        partSuperior.transform.localEulerAngles = -nouAngleAlpha;
        partInferior.transform.localEulerAngles = nouAngleAlpha;

        pivotSuperior.transform.localEulerAngles = nouAngleBeta;
        pivotInferior.transform.localEulerAngles = -nouAngleBeta;
    }
}
