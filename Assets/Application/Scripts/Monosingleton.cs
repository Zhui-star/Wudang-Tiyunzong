using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HTLibrary.Application
{
    /// <summary>
    /// 单例模式
    /// </summary>
    public class Monosingleton<T>: MonoBehaviour where T:MonoBehaviour
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if(instance==null)
                {
                    instance = FindObjectOfType<T>();
                    if(instance==null)
                    {
                        GameObject creatTGo = new GameObject(typeof(T).Name);
                        instance= creatTGo.AddComponent<T>();
                    }
                    
                }

                return instance;
            }
        }
    }

}
