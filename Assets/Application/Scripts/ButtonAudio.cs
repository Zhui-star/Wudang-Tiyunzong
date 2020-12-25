using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace HTLibrary.Application
{
    /// <summary>
    /// 处理按钮点击音效
    /// </summary>
    public class ButtonAudio : MonoBehaviour, IPointerDownHandler
    {
        public AudioClip clickClip;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (clickClip != null)
            {
                SoundManager.Instance.PlaySound(clickClip, transform.position, false);
            }
        }

  
    }

}
