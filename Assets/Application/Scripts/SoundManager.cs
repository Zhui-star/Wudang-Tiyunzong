using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HTLibrary.Application
{
    /// <summary>
    /// 音频管理器
    /// </summary>
    public class SoundManager :Monosingleton<SoundManager>
    {
        AudioSource _backgroundMusic;
        public float musicVolume=0.5f;
        public float sfxVolume = 0.5f;
        public bool sfxOn = true;
        public bool musicOn = true;
        /// <summary>
        /// 播放BGM
        /// </summary>
        /// <param name="music"></param>
        /// <param name="shouldBool"></param>
        public virtual void PlayBackgroundMusic(AudioSource music,bool shouldBool=true)
        {
            if(_backgroundMusic!=null)
            {
                _backgroundMusic.Stop();
            }

            _backgroundMusic = music;
            _backgroundMusic.volume = musicVolume;
            _backgroundMusic.loop = shouldBool;
            _backgroundMusic.Play();
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="sfx"></param>
        /// <param name="location"></param>
        /// <param name="loop"></param>
        /// <returns></returns>
        public virtual AudioSource PlaySound(AudioClip sfx,Vector3 location,bool loop=false)
        {
            if (!sfxOn) return null;

            GameObject temporaryAudioHost = new GameObject("TempAudio");

            temporaryAudioHost.transform.position = location;

            AudioSource audioSource=  temporaryAudioHost.AddComponent<AudioSource>() as AudioSource;
 
            audioSource.clip = sfx;

            audioSource.volume = sfxVolume;

            audioSource.loop = loop;

            audioSource.Play();

            if(!loop)
            {
                Destroy(temporaryAudioHost, sfx.length);
            }

            return audioSource;
        }

        /// <summary>
        /// 静音BGM
        /// </summary>
        public virtual void MuteBackGroundMusic()
        {
            if(_backgroundMusic!=null)
            {
                _backgroundMusic.mute = true;
            }
        }

        /// <summary>
        /// 解除静音
        /// </summary>
        public virtual void UnmuteBackGroundMusic()
        {
            if(_backgroundMusic!=null)
            {
                _backgroundMusic.mute = false;
            }
        }

        /// <summary>
        /// 设置BGM状态
        /// </summary>
        /// <param name="status"></param>
        public virtual void SetMusic(bool status)
        {
            musicOn = status;
            if(status)
            {
                UnmuteBackGroundMusic();
            }
            else
            {
                MuteBackGroundMusic();
            }
        }

        /// <summary>
        /// 开启BGM
        /// </summary>
        public virtual void MusicOn()
        {
            SetMusic(true);
        }

        /// <summary>
        /// BGM关闭
        /// </summary>
        public virtual void MusicOff()
        {
            SetMusic(false);
        }

        /// <summary>
        /// 设置音效状态
        /// </summary>
        /// <param name="status"></param>
        public virtual void SetSfx(bool status)
        {
            sfxOn = status;
        }
            

    }

}
