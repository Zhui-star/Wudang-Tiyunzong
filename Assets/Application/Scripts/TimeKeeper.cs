using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace HTLibrary.Application
{
    /// <summary>
    /// 时间计算器
    /// </summary>
    public class TimeKeeper
    {
        /// <summary>
        /// 得到当前时间 (秒)
        /// </summary>
        /// <returns></returns>
       public static long GetTimeStamp()
        {
            return    DateTime.UtcNow.Ticks / 10000000;
        }

        /// <summary>
        /// 得到2个时间差值
        /// </summary>
        /// <param name="timePast"></param>
        /// <param name="timeFurture"></param>
        /// <returns></returns>
        public static long TimeStampElasped(long timePast,long timeFurture)
        {
            return timeFurture - timePast;
        }
    }

}
