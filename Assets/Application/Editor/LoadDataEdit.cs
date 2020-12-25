using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace HTLibrary.Editor
{
    public class LoadDataEdit
    {
        [MenuItem("HTLibrary/Delete All local data")]
        static void DeleteLoacalData()
        {
            PlayerPrefs.DeleteAll();
        }
    }

}
