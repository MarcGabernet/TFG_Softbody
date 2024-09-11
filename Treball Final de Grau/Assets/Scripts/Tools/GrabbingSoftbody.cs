using Assets.SoftBody;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabbingSoftbody : MonoBehaviour
{
    public float radiImmediat = 0.05f;
    public float radiProper = 0.1f;

    public float percentatgeExtra = 50;

    XRBaseController controller;

    AnimacioEines estatEina;

    float valorTrigger;

    TocantSubcomponents[] pinces;

    public List<GameObject> partSuperior;
    public List<GameObject> partInferior;

    [HideInInspector]
    public List<GameObject> elementsEnComu;

    [HideInInspector]
    public List<GameObject> elementsEnganxats;

    SubcomponentsPropers[][] subcomponentsPropers;

    private String nameOfObject = null;

    private void Start()
    {
        pinces = GetComponentsInChildren<TocantSubcomponents>();
        estatEina = GetComponent<AnimacioEines>();
        elementsEnganxats = new();
        subcomponentsPropers = new SubcomponentsPropers[0][];
        controller = GetComponentInParent<XRBaseController>();
    }

    private void FixedUpdate()
    {
        valorTrigger = estatEina.triggerValue;

        partSuperior = pinces[0].tocant;
        partInferior = pinces[1].tocant;

        elementsEnComu = ElementsEnComu(partSuperior, partInferior);


        if (elementsEnComu.Count != 0 && valorTrigger > 0.01 && elementsEnganxats.Count == 0)
        {
            elementsEnganxats = elementsEnComu;
            EnganxaALesPinces(elementsEnganxats);
            subcomponentsPropers = DistanciesInicials(elementsEnganxats);
        }

        if(valorTrigger < 0.01 && elementsEnganxats.Count != 0)
        {
            DesenganxaDeLesPinces();
        }

        float intensitat = ComprovaDistanciaIApropaObjectes(percentatgeExtra, elementsEnganxats, subcomponentsPropers);
        controller.SendHapticImpulse(intensitat, Time.deltaTime);
    }

    List<GameObject> ElementsEnComu(List<GameObject> ps, List<GameObject> pi)
    {
        
        List<GameObject> llista = new();
        foreach (GameObject goS in ps)
        {
            foreach (GameObject goI in pi)
            {
                if (goI == goS && goI.layer == 3)
                {
                    if (!goI.GetComponent<Rigidbody>().isKinematic)
                    {
                        llista.Add(goS);
                        break;
                    }
                }

            }
        }

        return llista;
    }

    void EnganxaALesPinces(List<GameObject> llista)
    {
        if(llista.Count != 0)
        {
            nameOfObject = llista[0].transform.parent.transform.parent.name;
        }

        Collider[] nearSubcomponents = Physics.OverlapSphere(transform.GetChild(3).position, radiImmediat);
        foreach (Collider col in nearSubcomponents)
        {
            if (col.gameObject.layer == 3 && col.gameObject.transform.parent.transform.parent.name == nameOfObject)
            {
                llista.Add(col.gameObject);
            }
        }
        foreach (GameObject go in llista)
        {
            FixedJoint fj = transform.GetChild(3).AddComponent<FixedJoint>();
            fj.connectedBody = go.GetComponent<Rigidbody>();
        }
    }

    void DesenganxaDeLesPinces()
    {
        FixedJoint[] components = transform.GetChild(3).GetComponents<FixedJoint>();

        foreach (FixedJoint joints in components)
        {
            DestroyImmediate(joints);
        }
        elementsEnganxats.Clear();
    }

    public class SubcomponentsPropers
    {
        public float distancia;
        public GameObject subcomponent;
    }

    SubcomponentsPropers[][] DistanciesInicials(List<GameObject> elementsEnganxats)
    {
        SubcomponentsPropers[][] subcomponentsPropers = new SubcomponentsPropers[elementsEnganxats.Count][];

        for(int i=0; i < elementsEnganxats.Count; i++)
        {
            Collider[] hitColliders = Physics.OverlapSphere(elementsEnganxats[i].transform.position, radiProper);
            subcomponentsPropers[i] = new SubcomponentsPropers[hitColliders.Length];
            for (int j=0; j< hitColliders.Length; j++)
            {
                subcomponentsPropers[i][j] = new();
                subcomponentsPropers[i][j].subcomponent = hitColliders[j].gameObject;
                subcomponentsPropers[i][j].distancia = Vector3.Distance(elementsEnganxats[i].transform.position, hitColliders[j].gameObject.transform.position);
            }
        }

        return subcomponentsPropers;
    }

    float ComprovaDistanciaIApropaObjectes(float percentatgeAugmentMaxim, List<GameObject> elementsEnganxats, SubcomponentsPropers[][] elementsPropers)
    {
        float maxDistance;
        Vector3 direccio;

        float distancia;
        float distanciaMax;

        percentatgeAugmentMaxim *= 0.01f;
        percentatgeAugmentMaxim += 1;

        int elementsTotals = 0;
        int elementsEstirats = 0;

        for (int i = 0; i < elementsEnganxats.Count; i++)
        {
            maxDistance = percentatgeAugmentMaxim;
            for (int j = 0; j < elementsPropers[i].Length; j++)
            {
                elementsTotals++;
                distancia = Vector3.Distance(elementsEnganxats[i].transform.position, elementsPropers[i][j].subcomponent.transform.position);
                distanciaMax = elementsPropers[i][j].distancia * maxDistance;
                if (distanciaMax < distancia)
                {
                    if(elementsPropers[i][j].subcomponent.layer == 3 && !elementsPropers[i][j].subcomponent.GetComponent<Rigidbody>().isKinematic && elementsPropers[i][j].subcomponent.transform.parent.transform.parent.name == nameOfObject)
                    {
                        elementsEstirats++;
                        direccio = Vector3.Normalize(elementsEnganxats[i].transform.position - elementsPropers[i][j].subcomponent.transform.position);
                        elementsPropers[i][j].subcomponent.transform.position += direccio * (distancia - distanciaMax);
                    }
                }
            }
        }
        float intensitat = (float)elementsEstirats / (float)elementsTotals;
        return intensitat;
    }
}
