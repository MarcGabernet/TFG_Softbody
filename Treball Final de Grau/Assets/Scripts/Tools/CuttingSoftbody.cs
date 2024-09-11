using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.SoftBody
{
    public class CuttingSoftbody : MonoBehaviour
    {
        [SerializeField]
        private InputActionProperty button;
        [HideInInspector]
        public float buttonValue;

        [SerializeField]
        private float timerDuration = 0.5f;

        [SerializeField]
        private float radiComprovacio = 0.01f;

        [SerializeField]
        private float grauReduccioSubcomponents = 1f;

        AnimacioEines estatEina;
        float triggerValue;
        GrabbingSoftbody grabbing;
        List<GameObject> elementsEnComu;

        private bool isTimerRunning = false;
        private GameObject objectToCut;

        private void Start()
        {
            estatEina = GetComponent<AnimacioEines>();
            grabbing = GetComponent<GrabbingSoftbody>();
        }

        void Update()
        {
            buttonValue = button.action.ReadValue<float>();
            triggerValue = estatEina.triggerValue;

            elementsEnComu = grabbing.elementsEnComu;

            objectToCut = ComprovaEspaiTall(objectToCut);

            if (/*triggerValue < 0.001 &&*/ buttonValue == 1 && !isTimerRunning && objectToCut != null /*&& elementsEnComu.Count == 0*/)
            {/*
                grabbing.elementsEnComu = new();
                grabbing.partSuperior = null;
                grabbing.partInferior = null;*/
                CreaTall(objectToCut);
                StartCoroutine(Buffer());
            }
        }

        GameObject ComprovaEspaiTall(GameObject go)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.GetChild(4).position, radiComprovacio);
            foreach (Collider col in colliders)
            {
                if (col.gameObject.layer == 3)
                {
                    go = col.gameObject.transform.parent.transform.parent.gameObject;
                    return go;
                }
            }
            return null;
        }

        IEnumerator Buffer()
        {
            isTimerRunning = true;

            yield return new WaitForSeconds(timerDuration);

            isTimerRunning = false;
        }

        void CreaTall(GameObject softbody)
        {
            //direcció perpendicular a l'eina
            Vector3 normal = transform.up.normalized;

            //Transform the normal so that it is aligned with the object we are slicing's transform.
            Vector3 transformedNormal = ((Vector3)(softbody.transform.localToWorldMatrix.transpose * normal)).normalized;

            //Punt des d'on creem el tall
            Vector3 transformedStartingPoint = softbody.transform.InverseTransformPoint(transform.GetChild(3).transform.position);

            Plane plane = new Plane();
            plane.SetNormalAndPosition(
                    transformedNormal,
                    transformedStartingPoint);

            var direction = Vector3.Dot(Vector3.up, transformedNormal);

            //Flip the plane so that we always know which side the positive mesh is on
            if (direction < 0)
            {
                plane = plane.flipped;
            }

            if (softbody.GetComponent<MeshFilter>() != null)
            {
                GameObject[] slices = Slicer.Slice(plane, softbody);

                AssignaSoftBodyIConvertir(softbody, slices[1], slices[0]);

                Destroy(softbody);
            }
        }

        void AssignaSoftBodyIConvertir(GameObject pare, GameObject fill1, GameObject fill2)
        {
            List<GameObject> fills = new()
            {
                fill1,
                fill2
            };

            Rigidbody rb = pare.GetComponent<Rigidbody>();
            SpringJoint sj = pare.GetComponent<SpringJoint>();
            SoftBody sb = pare.GetComponent<SoftBody>();

            foreach (GameObject fill in fills)
            {
                SoftBody soft = fill.AddComponent<SoftBody>();
                //SoftBody
                soft.radi = sb.radi / grauReduccioSubcomponents;
                soft.distancia = sb.distancia / grauReduccioSubcomponents;
                soft.connexionsExtra = sb.connexionsExtra;
                soft.meshVisible = true;
                soft.material = sb.material;
                soft.material2 = sb.material2;
                soft.componentEsfera = sb.componentEsfera;

                Rigidbody rb1 = fill.GetComponent<Rigidbody>();
                //Rigidbody
                rb1.mass = rb.mass;
                rb1.drag = rb.drag;
                rb1.angularDrag = rb.angularDrag;
                rb1.useGravity = rb.useGravity;
                rb1.isKinematic = rb.isKinematic;
                rb1.interpolation = rb.interpolation;
                rb1.collisionDetectionMode = rb.collisionDetectionMode;
                rb1.constraints = rb.constraints;
                rb1.collisionDetectionMode = rb.collisionDetectionMode;

                SpringJoint joint = fill.GetComponent<SpringJoint>();
                //SpringJoint
                joint.spring = sj.spring;
                joint.damper = sj.damper;
                joint.minDistance = sj.minDistance;
                joint.maxDistance = sj.maxDistance;
                joint.tolerance = sj.tolerance;
                joint.breakForce = sj.breakForce;
                joint.breakTorque = sj.breakTorque;
                joint.enableCollision = sj.enableCollision;
                joint.enablePreprocessing = sj.enablePreprocessing;
                joint.massScale = sj.massScale;
                joint.connectedMassScale = sj.connectedMassScale;
                joint.enableCollision = sj.enableCollision;

                fill.GetComponent<MeshCollider>().isTrigger = true;

                soft.ConvertirATou();
            }


        }
    }
}

