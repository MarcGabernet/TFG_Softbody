using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using Unity.VisualScripting;
using System.Linq;
using System;
using System.Reflection;
using static UnityEditor.PlayerSettings;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.SoftBody
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SpringJoint))]
    public class SoftBody : MonoBehaviour
    {
        [Header("Dimensions dels subcomponents")]

        //Radi de les esferes subcomponent
        public float radi = 0.2f;

        //Distancia entre subcomponents
        public float distancia = 0.2f;

        [Range(1, 10)]
        public int connexionsExtra = 2;


        [Header("Visual (PROVISIONAL)")]
        public Material material;

        public Material material2;

        public Mesh componentEsfera;

        public bool meshVisible;

        //Vertex que definiran on es creen els subcomponents
        private Vector3 vertexInferior;
        private Vector3 vertexSuperior;

        private Rigidbody rb;

        //Fills de l'objecte on guardarem tots els altres
        private GameObject subcomponents;
        private GameObject vertex;

        private GameObject[][][] coordenadesSubcomponents;
        private GameObject[][][] subcomponentsExteriors;

        //Tags
        private String buildingTag = "Building";
        private String softbodyTag = "Softbody";

        public void ConvertirATou()
        {
            //Per odernar-ho creem els fills on guardarem els components
            subcomponents = new();
            subcomponents.name = "Subcomponents";
            subcomponents.transform.parent = transform;

            vertex = new();
            vertex.name = "Vertexs";
            vertex.transform.parent = transform;

            //Declarem el rigidBody que farem servir pels subcomponents
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = false;

            //Calcular el domini d'intent de generació de boles
            MeshFilter mf = rb.GetComponent<MeshFilter>();
            vertexInferior = CalculsDeVertex(mf.sharedMesh)[0];
            vertexSuperior = CalculsDeVertex(mf.sharedMesh)[1];

            //objecte que representarà cada subcomponent del sistema
            GameObject go = ObjecteAInstanciar(rb);
            go.name = "ERROR: Codi no executat correctament";

            //Assegurem que el valor de distancia no faci petar l'ordenador
            distancia = ComprovarValorsValids(distancia);

            gameObject.tag = buildingTag;

            //Fem els procediments
            int numeroSubcomponents=GenerarSubcomponents(vertexInferior, vertexSuperior, go);
            CompletarSubcomponentsExteriors();
            CrearConnexionsIAssignaPes(numeroSubcomponents);
            CrearConnexioAlMesh();

            //Inhabilitem el components que no ens interessen de l'objecte inicial
            if (!meshVisible)
            {
                GetComponent<MeshRenderer>().enabled = false;
            }
            rb.isKinematic = true;
            DestroyImmediate(go);


            //Creem un nou collider global
            if (gameObject.GetComponent<MeshCollider>() == null)
            {
                GetComponent<Collider>().enabled = false; 
                MeshCollider mc = gameObject.AddComponent<MeshCollider>();
                mc.convex = true;
                mc.isTrigger = true;
            }

            gameObject.tag = softbodyTag;
        }

        Vector3[] CambiarEscalaAlsVertex(Vector3[] vertex)
        {
            for (int i = 0; i < vertex.Length; i++)
            {
                vertex[i] = Vector3.Scale(vertex[i], transform.localScale);
            }

            return vertex;
        }

        Vector3[] CalculsDeVertex(Mesh mesh)
        {
            Vector3[] VertexInferiorISuperior = new Vector3[2];

            Vector3[] vertexs = mesh.vertices;
            vertexs = CambiarEscalaAlsVertex(vertexs);

            float minX = vertexs[0].x; float maxX = vertexs[0].x;
            float minY = vertexs[0].y; float maxY = vertexs[0].y;
            float minZ = vertexs[0].z; float maxZ = vertexs[0].z;

            for (int i = 0; i < vertexs.Length; i++)
            {
                if (vertexs[i].x < minX)
                {
                    minX = vertexs[i].x;
                }
                if (vertexs[i].y < minY)
                {
                    minY = vertexs[i].y;
                }
                if (vertexs[i].z < minZ)
                {
                    minZ = vertexs[i].z;
                }
                if (vertexs[i].x > maxX)
                {
                    maxX = vertexs[i].x;
                }
                if (vertexs[i].y > maxY)
                {
                    maxY = vertexs[i].y;
                }
                if (vertexs[i].z > maxZ)
                {
                    maxZ = vertexs[i].z;
                }
            }

            VertexInferiorISuperior[0] = new Vector3(minX, minY, minZ);
            VertexInferiorISuperior[1] = new Vector3(maxX, maxY, maxZ);

            return VertexInferiorISuperior;
        }

        public GameObject ObjecteAInstanciar(Rigidbody rb)
        {
            GameObject obj = new();
            obj.layer = 3;

            //Rigidbody
            Rigidbody rb1 = obj.AddComponent<Rigidbody>();

            rb1.mass = rb.mass;
            rb1.drag = rb.drag;
            rb1.angularDrag = rb.angularDrag;
            rb1.useGravity = rb.useGravity;
            rb1.isKinematic = rb.isKinematic;
            rb1.interpolation = rb.interpolation;
            rb1.collisionDetectionMode = rb.collisionDetectionMode;
            rb1.constraints = rb.constraints;
            rb1.collisionDetectionMode = rb.collisionDetectionMode;

            return obj;
        }

        float ComprovarValorsValids(float dis)
        {
            if (dis < 0.001)
            {
                dis = 0.05f;
            }

            return dis;
        }

        int GenerarSubcomponents(Vector3 vertexInferior, Vector3 vertexSuperior, GameObject objecteInstanciat)
        {
            //Mantindrem constancia del numero de subcomponents generats
            int n = 0;

            //Creem les matrius tridimensionals on guardarem els objectes
            int[] coords = new int[3];
            coords[0] = NumeroDeSubcomponentsEnEix(vertexInferior.z, vertexSuperior.z); //z
            coords[1] = NumeroDeSubcomponentsEnEix(vertexInferior.x, vertexSuperior.x); //x
            coords[2] = NumeroDeSubcomponentsEnEix(vertexInferior.y, vertexSuperior.y); //y

            coordenadesSubcomponents = new GameObject[coords[0]][][];
            subcomponentsExteriors = new GameObject[coords[0]][][];

            for (int z = 0; z < coords[0]; z++)
            {
                coordenadesSubcomponents[z] = new GameObject[coords[1]][];
                subcomponentsExteriors[z] = new GameObject[coords[1]][];
                for (int x = 0; x < coords[1]; x++)
                {
                    coordenadesSubcomponents[z][x] = new GameObject[coords[2]];
                    subcomponentsExteriors[z][x] = new GameObject[coords[2]];
                    for (int y = 0; y < coords[2]; y++)
                    {
                        coordenadesSubcomponents[z][x][y] = null;
                        subcomponentsExteriors[z][x][y] = null;
                    }
                }
            }

            Vector3 pos = new(vertexInferior.x, vertexInferior.y, vertexInferior.z);

            coords[0] = 0; //z
            coords[1] = 0; //x
            coords[2] = 0; //y


            while (pos.y <= vertexSuperior.y)
            {
                while (pos.x <= vertexSuperior.x)
                {
                    while (pos.z <= vertexSuperior.z)
                    {
                        n = ComprovaSiDinsObjecte(objecteInstanciat, pos, coords, n);

                        pos.z += distancia;
                        coords[0] += 1;
                    }
                    pos.z = vertexInferior.z;
                    coords[0] = 0;

                    pos.x += distancia;
                    coords[1] += 1;

                }
                pos.z = vertexInferior.z;
                coords[0] = 0;
                pos.x = vertexInferior.x;
                coords[1] = 0;

                pos.y += distancia;
                coords[2] += 1;
            }
            //canviem la posició del fill "subcomponent" per a que les boles queden dins de l'objecte
            subcomponents.transform.position += transform.position;

            return n;
        }

        int NumeroDeSubcomponentsEnEix(float posicioInicialEnEix, float vertexSuperiorEnEix)
        {
            int i = 0;
            while (posicioInicialEnEix <= vertexSuperiorEnEix)
            {
                posicioInicialEnEix += distancia;
                i++;
            }

            return i;
        }

        int ComprovaSiDinsObjecte(GameObject objecteInstanciat, Vector3 pos, int[] coords, int n)
        {
            pos += transform.position;

            Collider[] hitColliders = Physics.OverlapSphere(pos, 0f);

            foreach (Collider collider in hitColliders)
            {
                if (collider.gameObject.CompareTag(buildingTag))
                {
                    n = InstanciarSubcomponent(objecteInstanciat, pos, coords, n);
                    return n;
                }
            }
            return n;
            /*
            if (hitColliders.Length > 0)
            {
                n = InstanciarSubcomponent(objecteInstanciat, pos, coords, n);
            }
            return n;*/
        }

        int InstanciarSubcomponent(GameObject objecteInstanciat, Vector3 pos, int[] coords, int n)
        {
            GameObject punt = Instantiate(objecteInstanciat, subcomponents.transform);
            punt.transform.position = pos - transform.position;
            punt.transform.localScale = Vector3.one * radi;

            punt.name = coords[0].ToString() + "_" + coords[1].ToString() + "_" + coords[2].ToString();
            coordenadesSubcomponents[coords[0]][coords[1]][coords[2]] = punt;
            n++;

            return n;
        }

        void CompletarSubcomponentsExteriors()
        {
            for (int z = 0; z < coordenadesSubcomponents.Length; z++)
            {
                for (int x = 0; x < coordenadesSubcomponents[0].Length; x++)
                {
                    for (int y = 0; y < coordenadesSubcomponents[0][0].Length; y++)
                    {
                        if (coordenadesSubcomponents[z][x][y] != null)
                        {
                            if (z == 0 || x == 0 || y == 0 || z == coordenadesSubcomponents.Length - 1 || x == coordenadesSubcomponents[0].Length - 1 || y == coordenadesSubcomponents[0][0].Length - 1)
                            {
                                AfegirColisioExterior(coordenadesSubcomponents[z][x][y]);
                                subcomponentsExteriors[z][x][y] = coordenadesSubcomponents[z][x][y];
                            }
                            else
                            {
                                if (coordenadesSubcomponents[z + 1][x][y] == null || coordenadesSubcomponents[z][x + 1][y] == null || coordenadesSubcomponents[z][x][y + 1] == null ||
                                    coordenadesSubcomponents[z - 1][x][y] == null || coordenadesSubcomponents[z][x - 1][y] == null || coordenadesSubcomponents[z][x][y - 1] == null)
                                {
                                    AfegirColisioExterior(coordenadesSubcomponents[z][x][y]);
                                    subcomponentsExteriors[z][x][y] = coordenadesSubcomponents[z][x][y];
                                }
                            }
                        }
                    }
                }
            }
        }

        void AfegirColisioExterior(GameObject obj)
        {
            obj.AddComponent<BoxCollider>();

            MeshFilter mf = obj.AddComponent<MeshFilter>();
            mf.mesh = componentEsfera;

            MeshRenderer mr = obj.AddComponent<MeshRenderer>();
            mr.material = material;
        }
        
        void CrearConnexionsIAssignaPes(int n)
        {
            int eixZ = coordenadesSubcomponents.Length;
            int eixX = coordenadesSubcomponents[0].Length;
            int eixY = coordenadesSubcomponents[0][0].Length;

            for (int z = 0; z < eixZ; z++)
            {
                for (int x = 0; x < eixX; x++)
                {
                    for (int y = 0; y < eixY; y++)
                    {
                        if(coordenadesSubcomponents[z][x][y] != null)
                        {
                            coordenadesSubcomponents[z][x][y].GetComponent<Rigidbody>().mass = GetComponent<Rigidbody>().mass / (float)n;

                            for (int j = 1; j <= connexionsExtra; j++)
                            {
                                //Eixos
                                if (z + j < eixZ)
                                {
                                    CreaConnexio(coordenadesSubcomponents[z][x][y], coordenadesSubcomponents[z + j][x][y]);
                                }
                                if (x + j < eixX)
                                {
                                    CreaConnexio(coordenadesSubcomponents[z][x][y], coordenadesSubcomponents[z][x + j][y]);
                                }
                                if (y + j < eixY)
                                {
                                    CreaConnexio(coordenadesSubcomponents[z][x][y], coordenadesSubcomponents[z][x][y + j]);
                                }
                            }
                            //Diagonals exteriors
                            if (subcomponentsExteriors[z][x][y] != null)
                            {
                                //Pla ZX
                                if (z + 1 < eixZ && x + 1 < eixX)
                                {
                                    CreaConnexio(subcomponentsExteriors[z][x][y], subcomponentsExteriors[z + 1][x + 1][y]);
                                }
                                if (z + 1 < eixZ && x > 0)
                                {
                                    CreaConnexio(subcomponentsExteriors[z][x][y], subcomponentsExteriors[z + 1][x - 1][y]);
                                }
                                //Pla XY
                                if (x + 1 < eixX && y + 1 < eixY)
                                {
                                    CreaConnexio(subcomponentsExteriors[z][x][y], subcomponentsExteriors[z][x + 1][y + 1]);
                                }
                                if (x > 0 && y + 1 < eixY)
                                {
                                    CreaConnexio(subcomponentsExteriors[z][x][y], subcomponentsExteriors[z][x - 1][y + 1]);
                                }
                                //Pla ZY
                                if (z + 1 < eixZ && y + 1 < eixY)
                                {
                                    CreaConnexio(subcomponentsExteriors[z][x][y], subcomponentsExteriors[z + 1][x][y + 1]);
                                }
                                if (z - 1 >= 0 && y + 1 < eixY)
                                {
                                    CreaConnexio(subcomponentsExteriors[z][x][y], subcomponentsExteriors[z - 1][x][y + 1]);
                                }
                            }
                        }
                    }
                }
            }
        }

        void CreaConnexio(GameObject objecteSortida, GameObject objecteArribada)
        {
            if(objecteArribada != null)
            {
                objecteSortida.AddComponent<SpringJoint>();
                SpringJoint[] joints = objecteSortida.GetComponents<SpringJoint>();
                SpringJoint sj = GetComponent<SpringJoint>();
                foreach (SpringJoint joint in joints)
                {
                    if (joint.connectedBody == null)
                    {
                        joint.connectedBody = objecteArribada.GetComponent<Rigidbody>();
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
                    }
                }
            }
        }

        void CrearConnexioAlMesh()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            Mesh mesh = mf.sharedMesh;

            GameObject[] meshPoints = new GameObject[mesh.vertices.Length];

            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                GameObject go = new();
                PropietatsMeshPoint(go, componentEsfera);

                GameObject meshPoint = Instantiate(go, vertex.transform);
                meshPoint.transform.position = Vector3.Scale(mesh.vertices[i], transform.localScale);
                meshPoint.name = i.ToString();
                meshPoints[i] = meshPoint;

                DestroyImmediate(go);
            }

            vertex.transform.position += transform.position;

            float radiProva;
            int maxIntents = 50;

            foreach (GameObject go in meshPoints)
            {
                radiProva = 0.75f;
                float increment =0.5f;
                Collider[] hitColliders = Physics.OverlapSphere(go.transform.position, radiProva);
                List<Collider> realSubcomponents = RealSubcomponents(hitColliders);
                while (realSubcomponents.Count == 0 && radiProva / increment < maxIntents)
                {
                    radiProva += increment;
                    hitColliders = Physics.OverlapSphere(go.transform.position, radiProva);
                    realSubcomponents = RealSubcomponents(hitColliders);
                }
                if (realSubcomponents.Count == 0)
                {
                    Debug.LogError("S'ha fallat en adjuntar els vèrtex al objecte");
                    break;
                }
                FixedJoint fj = go.AddComponent<FixedJoint>();
                fj.connectedBody = BolaMesPropera(realSubcomponents, go).GetComponent<Rigidbody>();
            }
        }

        List<Collider> RealSubcomponents(Collider[] hitColliders)
        {
            List<Collider> realSubcomponents = new();
            foreach (Collider col in hitColliders)
            {
                if (col.gameObject.layer == 3)
                {
                    if (col.gameObject.transform.parent.transform.parent.CompareTag(buildingTag))
                    {
                        realSubcomponents.Add(col);
                    }
                }
            }
            return realSubcomponents;
        }

        void PropietatsMeshPoint(GameObject go, Mesh mesh)
        {
            go.tag = "Vertex";
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.0000001f;
            rb.freezeRotation = true;
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.mass = 0;
            go.transform.localScale = Vector3.one * 0.025f;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.mesh = mesh;

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = material2;
        }

        Collider BolaMesPropera(List<Collider> objectesPropers, GameObject go)
        {
            Collider millorOpcio = null;
            float distaciaMesProximaAlQuadrat = Mathf.Infinity;
            Vector3 currentPosition = go.transform.position;
            foreach (Collider obj in objectesPropers)
            {
                Vector3 vectorFinsObjecte = obj.transform.position - currentPosition;
                float dSqrToTarget = vectorFinsObjecte.sqrMagnitude;
                if (dSqrToTarget < distaciaMesProximaAlQuadrat)
                {
                    distaciaMesProximaAlQuadrat = dSqrToTarget;
                    millorOpcio = obj;
                }
            }

            return millorOpcio;
        }


#if UNITY_EDITOR
        public static void ConvertirTou()
        {
            SoftBody objecteTou = Selection.activeGameObject.GetComponent<SoftBody>();
            objecteTou.ConvertirATou();
        }

        [MenuItem("Soft Body/ Convertir-Actualitzar Objecte")]
        public static void Actualitzar()
        {
            RevertirTou();
            ConvertirTou();
        }

        [MenuItem("Soft Body/Eliminar Components Elastics")]
        public static void RevertirTou()
        {

            SoftBody objecteTou = Selection.activeGameObject.GetComponent<SoftBody>();
            objecteTou.gameObject.GetComponent<MeshRenderer>().enabled = true;
            Collider[] colliders = objecteTou.gameObject.GetComponents<Collider>();
            if(colliders.Length == 1)
            {
                objecteTou.gameObject.GetComponent<Collider>().enabled = true;
            }
            else
            {
                foreach (Collider collider in colliders)
                {
                    if(collider is MeshCollider)
                    {
                        DestroyImmediate(collider);
                    }
                    else
                    {
                        objecteTou.gameObject.GetComponent<Collider>().enabled = true;
                    }
                }
            }
            while (objecteTou.transform.childCount > 0)
            {
                DestroyImmediate(objecteTou.transform.GetChild(0).gameObject);
            }
        }

#endif

        Vector3[] ActualitzaMesh(Vector3[] vertices)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = vertex.transform.GetChild(i).position;
                vertices[i] = Quaternion.Inverse(transform.rotation) * (vertices[i] - vertex.transform.position) + vertex.transform.position;
                vertices[i] -= transform.position;
                vertices[i].x /= transform.localScale.x;
                vertices[i].y /= transform.localScale.y;
                vertices[i].z /= transform.localScale.z;
            }

            return vertices;
        }

        Mesh mesh;
        Vector3[] vertices;

        void Start()
        {
            vertex = transform.GetChild(1).gameObject;
            mesh = GetComponent<MeshFilter>().mesh;
            vertices = mesh.vertices;
        }

        void Update()
        {
            MeshCollider meshCollider = GetComponent<MeshCollider>();
            mesh.vertices = ActualitzaMesh(vertices);
            mesh.RecalculateBounds();
            meshCollider.sharedMesh = mesh;
        }

    }
}