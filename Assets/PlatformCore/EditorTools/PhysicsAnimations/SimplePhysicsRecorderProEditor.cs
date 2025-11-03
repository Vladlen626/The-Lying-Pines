#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimplePhysicsRecorderPro))]
public class SimplePhysicsRecorderProEditor : Editor
{
	private GUIStyle _statusStyle;

	// состояния секций
	private bool showRecord = true;
	private bool showPhysics = true;
	private bool showRigidbody = true;
	private bool showImpact = true;
	private bool showSequence = true;
	private bool showRandom = false;

	public override void OnInspectorGUI()
	{
		var r = (SimplePhysicsRecorderPro)target;
		serializedObject.Update();

		if (_statusStyle == null)
		{
			_statusStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				alignment = TextAnchor.MiddleCenter,
				fontSize = 11
			};
		}

		// ===== Верхняя панель: статус и кнопки =====
		Color bar = r.isRecording ? new Color(0.8f, 0.1f, 0.1f) : new Color(0.3f, 0.3f, 0.3f);
		var barRect = EditorGUILayout.GetControlRect(false, 4);
		EditorGUI.DrawRect(barRect, bar);

		string status = r.isRecording ? $"● Recording ({r.targets.Count} targets)" : "Idle";
		var old = GUI.color; GUI.color = r.isRecording ? Color.red : Color.gray;
		EditorGUILayout.LabelField(status, _statusStyle);
		GUI.color = old;

		EditorGUILayout.Space(2);

		GUI.enabled = Application.isPlaying && r.isActiveAndEnabled;
		EditorGUILayout.BeginHorizontal();
		if (!r.isRecording)
		{
			if (GUILayout.Button("Start Recording", GUILayout.Height(26))) r.StartRecording();
		}
		else
		{
			if (GUILayout.Button("Stop Recording", GUILayout.Height(26))) r.StopRecording();
		}
		if (GUILayout.Button("Reset", GUILayout.Height(26), GUILayout.Width(80))) r.ResetTargets();
		EditorGUILayout.EndHorizontal();
		GUI.enabled = true;

		EditorGUILayout.Space(6);

		if (!r.targets.Exists(t => t != null))
			EditorGUILayout.HelpBox("Добавь хотя бы один объект в Targets.", MessageType.Warning);

		GUI.enabled = !r.isRecording;

		// ===== Targets =====
		EditorGUILayout.PropertyField(serializedObject.FindProperty("targets"), true);
		DrawSeparator();

		// ===== Сворачиваемые секции =====
		showRecord = EditorGUILayout.BeginFoldoutHeaderGroup(showRecord, "Record Settings");
		if (showRecord)
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("recordRate"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("keyPrecision"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("saveFolder"));
		}
		EditorGUILayout.EndFoldoutHeaderGroup();

		showPhysics = EditorGUILayout.BeginFoldoutHeaderGroup(showPhysics, "Physics Control");
		if (showPhysics)
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("gravityScale"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("physicsTimeScale"));
		}
		EditorGUILayout.EndFoldoutHeaderGroup();

		showRigidbody = EditorGUILayout.BeginFoldoutHeaderGroup(showRigidbody, "Rigidbody Settings");
		if (showRigidbody)
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("mass"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("drag"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("angularDrag"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("useGravity"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("isKinematic"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("constraints"));
		}
		EditorGUILayout.EndFoldoutHeaderGroup();

		showImpact = EditorGUILayout.BeginFoldoutHeaderGroup(showImpact, "Impact Settings");
		if (showImpact)
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("impactMode"));
			var mode = (SimplePhysicsRecorderPro.ImpactMode)serializedObject.FindProperty("impactMode").enumValueIndex;

			if (mode == SimplePhysicsRecorderPro.ImpactMode.Directional ||
			    mode == SimplePhysicsRecorderPro.ImpactMode.DirectionalAtPoint)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("impactSpace"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("impactDirection"));
			}
			if (mode == SimplePhysicsRecorderPro.ImpactMode.DirectionalAtPoint)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("pointMode"));
				if ((SimplePhysicsRecorderPro.PointMode)serializedObject.FindProperty("pointMode").enumValueIndex
				    == SimplePhysicsRecorderPro.PointMode.CustomTransform)
					EditorGUILayout.PropertyField(serializedObject.FindProperty("customPoint"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("pointJitterRadius"));
			}
			if (mode == SimplePhysicsRecorderPro.ImpactMode.RadialExplosion)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("explosionOrigin"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("explosionRadius"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("upwardsModifier"));
			}
			if (mode == SimplePhysicsRecorderPro.ImpactMode.TorqueSpin)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("torqueAxis"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("randomTorqueAxis"));
			}
			EditorGUILayout.PropertyField(serializedObject.FindProperty("impulseForce"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("spreadAngle"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("forceJitter"));
		}
		EditorGUILayout.EndFoldoutHeaderGroup();

		showSequence = EditorGUILayout.BeginFoldoutHeaderGroup(showSequence, "Sequencing");
		if (showSequence)
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("enableSecondHit"));
			if (serializedObject.FindProperty("enableSecondHit").boolValue)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("secondHitDelay"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("secondHitMultiplier"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("secondHitMode"));
			}
		}
		EditorGUILayout.EndFoldoutHeaderGroup();

		showRandom = EditorGUILayout.BeginFoldoutHeaderGroup(showRandom, "Random Settings");
		if (showRandom)
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("deterministicRandom"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("randomSeed"));
		}
		EditorGUILayout.EndFoldoutHeaderGroup();

		GUI.enabled = true;
		EditorGUILayout.Space(6);
		EditorGUILayout.LabelField($"Recorded Time: {r.time:F2}s", EditorStyles.miniLabel);

		serializedObject.ApplyModifiedProperties();
	}

	private void DrawSeparator()
	{
		var rect = EditorGUILayout.GetControlRect(false, 1);
		EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.08f));
	}
}
#endif