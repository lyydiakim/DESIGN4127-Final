using UnityEditor;
using UnityEngine;

// Template/Example Editor - Uncomment and ensure MyComponent class exists in Runtime assembly
// [CustomEditor(typeof(MyComponent))]
public class MyComponentEditor : Editor
{
    // For foldouts or toggles, you can store states here
    private bool showAdvancedOptions;

    public override void OnInspectorGUI()
    {
        // Get a reference to the script
        // MyComponent myComponent = (MyComponent)target; // Uncomment when MyComponent is available

        // Update the serialized object (required for custom Editors)
        serializedObject.Update();

        // 1. Draw some fields normally
        EditorGUILayout.PropertyField(serializedObject.FindProperty("alwaysVisible"));

        // 2. Draw a toggle to decide if we show advanced stuff
        //    (Alternatively, you could use a foldout, see below)
        // myComponent.showAdvanced = EditorGUILayout.Toggle("Show Advanced?", myComponent.showAdvanced); // Uncomment when MyComponent is available

        // 3. Conditionally show or hide advanced fields
        // if (myComponent.showAdvanced) // Uncomment when MyComponent is available
        // {
        //     EditorGUILayout.PropertyField(serializedObject.FindProperty("advancedOption1"));
        //     EditorGUILayout.PropertyField(serializedObject.FindProperty("advancedOption2"));
        // }

        // Apply modifications
        serializedObject.ApplyModifiedProperties();
    }
}
