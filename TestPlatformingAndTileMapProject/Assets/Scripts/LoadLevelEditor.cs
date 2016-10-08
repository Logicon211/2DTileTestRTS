using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(Level))]
public class LoadLevelEditor : Editor {

	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		Level loadLevelScript = (Level )target;
	
		if(GUILayout.Button ("Load Level")) {
			loadLevelScript.loadAtStartUp = false;
			loadLevelScript.ClearLevelFromEditor();
			loadLevelScript.InstantiateLevel();
		}

		if(GUILayout.Button ("Clear Level")) {
			loadLevelScript.ClearLevelFromEditor();
		}
	}

	// Use this for initialization
	//void Start () {
	
	//}
	
	// Update is called once per frame
	//void Update () {
	
	//}
}
#endif
