using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HTLibrary.Application
{
    /// <summary>
    /// UI集体控制器 
    /// </summary>
    public class UIController : MonoBehaviour
    {
        public GameObject[] UIList;

        private void Awake()
        {
            foreach(var temp in UIList)
            {
                temp.SetActive(true);
            }
        }

        private void Start()
        {
            foreach(var temp in UIList)
            {
                temp.SetActive(false);
            }
        }
    }

}
