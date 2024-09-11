using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


namespace Assets.SoftBody
{
    public class TocantSubcomponents : MonoBehaviour
    {
        [HideInInspector]
        public List<GameObject> tocant;

        CuttingSoftbody cutting;

        private void Start()
        {
            tocant = new List<GameObject>();
            cutting = transform.parent.transform.parent.transform.parent.transform.parent.transform.parent.GetComponentInParent<CuttingSoftbody>();
        }

        private void FixedUpdate()
        {
            if(cutting.buttonValue == 1)
            {
                tocant.Clear();
            } 
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == 3)
            {
                tocant.Add(other.gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            tocant.Remove(other.gameObject);
        }

    }
}
