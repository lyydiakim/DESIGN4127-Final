using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Station))]
public class SA_StationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        Station station = (Station)target;

        EditorGUILayout.Space(4);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("stationData"));
        if (station.stationData == null)
        {
            EditorGUILayout.HelpBox(
                "Assign a StationDataSO to configure this station. Consume, produce, capital, goals, and timing are all defined in the StationDataSO asset.",
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Behavior (consume, produce, capital, goals, timing) is configured in the StationDataSO asset above.",
                MessageType.Info);
        }
        EditorGUILayout.Space(4);

        DrawDefaultInspectorExcluding("stationData");
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDefaultInspectorExcluding(string propertyToExclude)
    {
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (iterator.name == propertyToExclude) continue;
            EditorGUILayout.PropertyField(iterator, true);
        }
    }
}
