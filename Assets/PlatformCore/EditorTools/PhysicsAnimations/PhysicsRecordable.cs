using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SimplePhysicsRecorderPro : MonoBehaviour
{
	// ========= Targets =========
	[Header("Targets")] public List<GameObject> targets = new();

	// ========= Record Settings =========
	[Header("Record Settings")] public float recordRate = 60f;
	public float keyPrecision = 0.0005f;
	public string saveFolder = "Assets/BakedPhysics";

	// ========= Physics Control =========
	[Header("Physics Control")] [Tooltip("Множитель тяжести поверх глобальной Physics.gravity")]
	public float gravityScale = 1f;

	[Tooltip("Локальный множитель дельта-времени физики для таймера записи")]
	public float physicsTimeScale = 1f;

	// ========= Rigidbody Settings =========
	[Header("Rigidbody Settings (автоматически добавляется)")]
	public float mass = 10f;

	public float drag = 0f;
	public float angularDrag = 0.05f;
	public bool useGravity = true;
	public bool isKinematic = false;
	public RigidbodyConstraints constraints = RigidbodyConstraints.None;

	// ========= Impact Settings (NEW) =========
	public enum ImpactMode
	{
		Directional,
		DirectionalAtPoint,
		RadialExplosion,
		TorqueSpin
	}

	public ImpactMode impactMode = ImpactMode.Directional;

	[Header("Directional")] public Space impactSpace = Space.World;
	public Vector3 impactDirection = Vector3.right;

	[Tooltip("Сила импульса (Directional/AtPoint/Torque)")]
	public float impulseForce = 5f;

	[Tooltip("Разброс направления в градусах вокруг impactDirection")]
	public float spreadAngle = 0f;

	[Tooltip("Джиттер силы в долях (0..1)")] [Range(0f, 1f)]
	public float forceJitter = 0.0f;

	public enum PointMode
	{
		CenterOfBounds,
		RandomOnBounds,
		CustomTransform
	}

	public PointMode pointMode = PointMode.CenterOfBounds;
	public Transform customPoint;

	[Tooltip("Случайный сдвиг точки в пределах радиуса")]
	public float pointJitterRadius = 0f;

	[Header("Explosion")] public Transform explosionOrigin;
	public float explosionRadius = 3f;
	public float upwardsModifier = 0.2f;

	[Header("Torque Spin")] public Vector3 torqueAxis = Vector3.up;
	public bool randomTorqueAxis = false;

	[Header("Sequencing")] [Tooltip("Включить второй удар через задержку (например, \"добить\" накренившийся объект)")]
	public bool enableSecondHit = false;

	public float secondHitDelay = 0.35f;

	[Tooltip("Во сколько раз умножить силу второго удара")]
	public float secondHitMultiplier = 0.5f;

	public ImpactMode secondHitMode = ImpactMode.RadialExplosion;

	[Header("Random")] public bool deterministicRandom = true;
	public int randomSeed = 12345;

	// ========= Runtime =========
	[HideInInspector] public bool isRecording;
	[HideInInspector] public float time;

	private bool secondHitApplied;
	private float defaultFixedDelta;

	private Dictionary<GameObject, List<Vector3>> positions = new();
	private Dictionary<GameObject, List<Quaternion>> rotations = new();
	private Dictionary<GameObject, Vector3> startPositions = new();
	private Dictionary<GameObject, Quaternion> startRotations = new();

	private System.Random _rand;

	private System.Random Rand => _rand ??= new System.Random();

#if UNITY_EDITOR
	// --- SAFE STOP при выходе из Play Mode ---
	[InitializeOnLoadMethod]
	private static void RegisterPlayModeHandler()
	{
		EditorApplication.playModeStateChanged += state =>
		{
			if (state == PlayModeStateChange.ExitingPlayMode)
			{
				var recorders = Object.FindObjectsOfType<SimplePhysicsRecorderPro>();
				foreach (var r in recorders)
				{
					if (r.isRecording)
						r.StopRecording();
				}
			}
		};
	}
#endif

	void Start()
	{
		if (!Application.isPlaying) return;
		foreach (var t in targets)
		{
			if (!t) continue;
			startPositions[t] = t.transform.position;
			startRotations[t] = t.transform.rotation;
		}
	}

	void FixedUpdate()
	{
		if (!isRecording || !Application.isPlaying) return;

		float dt = Time.fixedDeltaTime * physicsTimeScale;
		time += dt;

		// Второй удар по таймеру (опционально)
		if (enableSecondHit && !secondHitApplied && time >= secondHitDelay)
		{
			ApplyImpactToTargets(secondHitMode, secondHitMultiplier);
			secondHitApplied = true;
		}

		foreach (var t in targets)
		{
			if (!t || !positions.ContainsKey(t)) continue;

			var rb = t.GetComponent<Rigidbody>();
			if (rb)
				rb.AddForce(Physics.gravity * (gravityScale - 1f) * rb.mass, ForceMode.Acceleration);

			positions[t].Add(t.transform.localPosition);
			rotations[t].Add(t.transform.localRotation);
		}
	}

	public void StartRecording()
	{
		if (targets.Count == 0)
		{
			Debug.LogError("[Recorder] No targets assigned!");
			return;
		}

		_rand = deterministicRandom ? new System.Random(randomSeed) : new System.Random();

		defaultFixedDelta = Time.fixedDeltaTime;
		Time.fixedDeltaTime = 1f / recordRate;

		positions.Clear();
		rotations.Clear();

		foreach (var t in targets)
		{
			if (!t) continue;

			var rb = t.GetComponent<Rigidbody>();
			if (!rb) rb = t.AddComponent<Rigidbody>();

			rb.mass = mass;
			rb.linearDamping = drag;
			rb.angularDamping = angularDrag;
			rb.useGravity = useGravity;
			rb.isKinematic = isKinematic;
			rb.constraints = constraints;

			positions[t] = new List<Vector3>();
			rotations[t] = new List<Quaternion>();
			startPositions[t] = t.transform.position;
			startRotations[t] = t.transform.rotation;
		}

		time = 0f;
		secondHitApplied = false;
		isRecording = true;

		// Первый удар сразу при старте
		ApplyImpactToTargets(impactMode, 1f);

		Debug.Log("[Recorder] Recording started.");
	}

	public void StopRecording()
	{
		if (!isRecording)
		{
			Debug.LogWarning("[Recorder] Not currently recording.");
			return;
		}

		isRecording = false;
		Time.fixedDeltaTime = defaultFixedDelta;
		Debug.Log($"[Recorder] Recording stopped. Duration: {time:F2}s");

#if UNITY_EDITOR
		SaveClips();
#endif
	}

	public void ResetTargets()
	{
		if (isRecording)
			StopRecording();

		foreach (var t in targets)
		{
			if (!t) continue;
			var rb = t.GetComponent<Rigidbody>();
			if (rb) DestroyImmediate(rb);

			if (startPositions.TryGetValue(t, out var pos))
				t.transform.position = pos;
			if (startRotations.TryGetValue(t, out var rot))
				t.transform.rotation = rot;
		}

		Debug.Log("[Recorder] Targets reset.");
	}

	// ===== Impacts ==========================================================

	private void ApplyImpactToTargets(ImpactMode mode, float forceMul)
	{
		foreach (var t in targets)
		{
			if (!t) continue;
			var rb = t.GetComponent<Rigidbody>();
			if (!rb) continue;

			switch (mode)
			{
				case ImpactMode.Directional:
				{
					Vector3 dir = GetJitteredDirection(t);
					float f = GetJitteredForce(impulseForce) * forceMul;
					rb.AddForce(dir * f, ForceMode.Impulse);
					break;
				}
				case ImpactMode.DirectionalAtPoint:
				{
					Vector3 dir = GetJitteredDirection(t);
					float f = GetJitteredForce(impulseForce) * forceMul;
					Vector3 point = ResolveImpactPoint(t);
					rb.AddForceAtPosition(dir * f, point, ForceMode.Impulse);
					break;
				}
				case ImpactMode.RadialExplosion:
				{
					Vector3 origin = explosionOrigin ? explosionOrigin.position : transform.position;
					float f = GetJitteredForce(impulseForce) * forceMul;
					rb.AddExplosionForce(f, origin, Mathf.Max(0.01f, explosionRadius), upwardsModifier,
						ForceMode.Impulse);
					break;
				}
				case ImpactMode.TorqueSpin:
				{
					Vector3 axis = randomTorqueAxis ? RandomOnUnitSphere() : torqueAxis.normalized;
					float f = GetJitteredForce(impulseForce) * forceMul;
					rb.AddTorque(axis * f, ForceMode.Impulse);
					break;
				}
			}
		}
	}

	private Vector3 GetJitteredDirection(GameObject t)
	{
		Vector3 baseDir = impactSpace == Space.World
			? impactDirection
			: t.transform.TransformDirection(impactDirection);
		if (baseDir.sqrMagnitude < 1e-6f) baseDir = Vector3.right;

		if (spreadAngle <= 0.001f) return baseDir.normalized;

		// Случайный конус вокруг baseDir
		Vector3 rand = RandomOnUnitSphere();
		Quaternion twist = Quaternion.AngleAxis(RandomRange(-spreadAngle, spreadAngle),
			Vector3.Cross(baseDir, rand).normalized);
		return (twist * baseDir).normalized;
	}

	private Vector3 ResolveImpactPoint(GameObject t)
	{
		Vector3 p = t.transform.position;

		switch (pointMode)
		{
			case PointMode.CenterOfBounds:
			{
				if (TryGetWorldBounds(t, out var b)) p = b.center;
				else p = t.transform.position;
				break;
			}
			case PointMode.RandomOnBounds:
			{
				if (TryGetWorldBounds(t, out var b))
				{
					// Случайная точка на поверхности AABB
					Vector3 min = b.min;
					Vector3 max = b.max;
					// Выбираем грань
					int face = RandomRangeInt(0, 6);
					float x = RandomRange(min.x, max.x);
					float y = RandomRange(min.y, max.y);
					float z = RandomRange(min.z, max.z);
					switch (face)
					{
						case 0: x = min.x; break;
						case 1: x = max.x; break;
						case 2: y = min.y; break;
						case 3: y = max.y; break;
						case 4: z = min.z; break;
						case 5: z = max.z; break;
					}

					p = new Vector3(x, y, z);
				}
				else p = t.transform.position;

				break;
			}
			case PointMode.CustomTransform:
			{
				p = customPoint ? customPoint.position : t.transform.position;
				break;
			}
		}

		if (pointJitterRadius > 0f)
		{
			p += RandomOnUnitSphere() * pointJitterRadius;
		}

		return p;
	}

	private bool TryGetWorldBounds(GameObject go, out Bounds bounds)
	{
		var cols = go.GetComponentsInChildren<Collider>();
		if (cols.Length > 0)
		{
			bounds = cols[0].bounds;
			for (int i = 1; i < cols.Length; i++)
				bounds.Encapsulate(cols[i].bounds);
			return true;
		}

		var rends = go.GetComponentsInChildren<Renderer>();
		if (rends.Length > 0)
		{
			bounds = rends[0].bounds;
			for (int i = 1; i < rends.Length; i++)
				bounds.Encapsulate(rends[i].bounds);
			return true;
		}

		bounds = new Bounds(go.transform.position, Vector3.zero);
		return false;
	}

	private float GetJitteredForce(float baseForce)
	{
		if (forceJitter <= 0f) return baseForce;
		float k = 1f + RandomRange(-forceJitter, forceJitter);
		return Mathf.Max(0f, baseForce * k);
	}

	private Vector3 RandomOnUnitSphere()
	{
		float u = (float)Rand.NextDouble();
		float v = (float)Rand.NextDouble();
		float theta = 2f * Mathf.PI * u;
		float phi = Mathf.Acos(2f * v - 1f);
		return new Vector3(
			Mathf.Sin(phi) * Mathf.Cos(theta),
			Mathf.Sin(phi) * Mathf.Sin(theta),
			Mathf.Cos(phi)
		);
	}

	private float RandomRange(float a, float b)
	{
		return a + (float)Rand.NextDouble() * (b - a);
	}

	private int RandomRangeInt(int minInclusive, int maxExclusive)
	{
		return Rand.Next(minInclusive, maxExclusive);
	}

	// ===== Saving ============================================================

#if UNITY_EDITOR
#if UNITY_EDITOR
	private void SaveClips()
	{
		if (!AssetDatabase.IsValidFolder(saveFolder))
		{
			System.IO.Directory.CreateDirectory(saveFolder);
			AssetDatabase.Refresh();
		}

		foreach (var kvp in positions)
		{
			var target = kvp.Key;
			if (!target) continue;

			var posList = kvp.Value;
			var rotList = rotations[target];

			// базовое имя файла без timestamp
			string baseName = $"{target.name}_Baked";
			string clipPath = $"{saveFolder}/{baseName}.anim";

			// если уже существует, находим свободное имя
			int index = 1;
			while (System.IO.File.Exists(clipPath))
			{
				clipPath = $"{saveFolder}/{baseName}_{index}.anim";
				index++;
			}

			var clip = new AnimationClip { frameRate = recordRate };

			AddCurveOptimized(clip, "localPosition.x", posList, v => v.x);
			AddCurveOptimized(clip, "localPosition.y", posList, v => v.y);
			AddCurveOptimized(clip, "localPosition.z", posList, v => v.z);
			AddCurveOptimized(clip, "localRotation.x", rotList, r => r.x);
			AddCurveOptimized(clip, "localRotation.y", rotList, r => r.y);
			AddCurveOptimized(clip, "localRotation.z", rotList, r => r.z);
			AddCurveOptimized(clip, "localRotation.w", rotList, r => r.w);

			AssetDatabase.CreateAsset(clip, clipPath);
			Debug.Log($"[Recorder] Saved: {clipPath}");
		}

		AssetDatabase.SaveAssets();
	}
#endif


	private void AddCurveOptimized<T>(AnimationClip clip, string property, List<T> list, System.Func<T, float> selector)
	{
		if (list.Count == 0) return;

		var curve = new AnimationCurve();
		float dt = 1f / recordRate;
		float prev = selector(list[0]);

		for (int i = 0; i < list.Count; i++)
		{
			float val = selector(list[i]);
			if (Mathf.Abs(val - prev) > keyPrecision || i == 0 || i == list.Count - 1)
			{
				curve.AddKey(i * dt, val);
				prev = val;
			}
		}

		clip.SetCurve("", typeof(Transform), property, curve);
	}
#endif

	// ===== Мини-HUD во время записи =========================================
	void OnGUI()
	{
		if (!Application.isPlaying || !isRecording) return;
		GUI.color = new Color(1f, 0.2f, 0.2f, 1f);
		GUI.Label(new Rect(10, 10, 280, 24), $"● Recording  {time:F1}s");
	}

	// ===== Gizmos: превью удара =============================================
#if UNITY_EDITOR
#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		if (targets == null || targets.Count == 0)
			return;

		// Цвета
		Color mainColor = new Color(1f, 0.35f, 0.15f, 0.9f);
		Color helperColor = new Color(1f, 0.75f, 0.25f, 0.35f);
		Color lineColor = new Color(1f, 0.4f, 0.15f, 0.75f);

		// Удобное смещение, чтобы стрелки не сливались
		float arrowLength = Mathf.Clamp(impulseForce * 0.3f, 0.5f, 2f);

		foreach (var t in targets)
		{
			if (!t) continue;

			// позиция для отрисовки
			Vector3 pos = t.transform.position;

			switch (impactMode)
			{
				case ImpactMode.Directional:
					Gizmos.color = mainColor;
					Vector3 dir = (impactSpace == Space.World)
						? impactDirection.normalized
						: t.transform.TransformDirection(impactDirection.normalized);

					Gizmos.DrawRay(pos, dir * arrowLength);
					Handles.color = mainColor;
					Handles.ArrowHandleCap(0, pos, Quaternion.LookRotation(dir), arrowLength, EventType.Repaint);
					break;

				case ImpactMode.DirectionalAtPoint:
					Vector3 point = customPoint ? customPoint.position : pos;
					Gizmos.color = helperColor;
					Gizmos.DrawSphere(point, 0.05f);
					Gizmos.color = lineColor;
					Vector3 dir2 = (impactSpace == Space.World)
						? impactDirection.normalized
						: t.transform.TransformDirection(impactDirection.normalized);
					Gizmos.DrawLine(point, point + dir2 * arrowLength);
					Handles.color = mainColor;
					Handles.ArrowHandleCap(0, point + dir2 * arrowLength * 0.9f, Quaternion.LookRotation(dir2),
						arrowLength * 0.2f, EventType.Repaint);
					break;

				case ImpactMode.RadialExplosion:
					Vector3 origin = explosionOrigin ? explosionOrigin.position : transform.position;
					Gizmos.color = helperColor;
					Gizmos.DrawWireSphere(origin, explosionRadius);
					Gizmos.color = lineColor;
					Gizmos.DrawLine(origin, pos);
					break;

				case ImpactMode.TorqueSpin:
					Vector3 axis = randomTorqueAxis
						? Vector3.up
						: (torqueAxis.sqrMagnitude < 1e-6f ? Vector3.up : torqueAxis.normalized);
					Gizmos.color = mainColor;
					Gizmos.DrawRay(pos, axis * arrowLength);
					Handles.color = mainColor;
					Handles.DrawWireDisc(pos, axis, 0.3f);
					break;
			}
		}

		// Показываем зону второго удара
		if (enableSecondHit && secondHitDelay > 0)
		{
			Handles.color = new Color(0.2f, 0.9f, 0.4f, 0.6f);
			Handles.Label(transform.position + Vector3.up * 1f, $"Second hit in {secondHitDelay:F2}s");
		}
	}
#endif

#endif
}