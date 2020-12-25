using System;
using System.Linq;
using System.Reflection;

namespace Dreamteck.Forever.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(RemoteLevel))]
    public class RemoteLevelEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            RemoteLevel remoteLvl = (RemoteLevel)target;
            serializedObject.Update();
            SerializedProperty usePooling = serializedObject.FindProperty("_usePooling");
            if (GUILayout.Button("Edit Sequence", GUILayout.Height(50)))
            {
                SequenceEditWindow window = EditorWindow.GetWindow<SequenceEditWindow>(true);
                window.Init(((RemoteLevel)target).sequenceCollection, target, OnApplySequences);
            }
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(usePooling);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (!Application.isPlaying)
                {
                    HandlePool(usePooling.boolValue);
                }
            }
        }

        private void HandlePool(bool usePool)
        {
            RemoteLevel remoteLvl = (RemoteLevel)target;
            for (int i = 0; i < remoteLvl.sequenceCollection.sequences.Length; i++)
            {
                if (usePool)
                {
                    remoteLvl.sequenceCollection.sequences[i].EditorDeployPool(remoteLvl.transform);
                } else
                {
                    remoteLvl.sequenceCollection.sequences[i].EditorDismantlePool();
                }
            }
        }

        private void OnApplySequences(SegmentSequence[] sequences)
        {
            RemoteLevel level = (RemoteLevel)target;
            level.sequenceCollection.sequences = sequences;
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                serializedObject.Update();
                SerializedProperty usePooling = serializedObject.FindProperty("_usePooling");
                HandlePool(usePooling.boolValue);
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                serializedObject.Update();
                SerializedProperty usePooling = serializedObject.FindProperty("_usePooling");
                HandlePool(usePooling.boolValue);
            }
        }
    }
}
