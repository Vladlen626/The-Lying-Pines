// Assets/_Main/Scripts/Collectibles/CollectibleView.cs
using UnityEngine;

namespace _Main.Scripts.Collectibles
{
	[RequireComponent(typeof(Collider))]
	public sealed class CollectibleView : MonoBehaviour
	{
		[Header("Data")]
		public CollectibleKind Kind = CollectibleKind.Crumb;
		public int Amount = 1;

		[Header("Setup")]
		[SerializeField] private Transform _visual;
		[SerializeField] private float _magnetRadius = 3f;
		[SerializeField] private float _contactRadius = 0.25f;

		private Collider _col;

		private void Awake()
		{
			_col = GetComponent<Collider>();
			_col.isTrigger = true;
			if (!_visual) _visual = transform;
		}
		
		public Vector3 WorldPos
		{
			get => _visual.position;
			set => _visual.position = value;
		}

		public float MagnetRadius => _magnetRadius;
		public float ContactRadius => _contactRadius;

		public void SetIdleOffset(float localY)
		{
			var p = _visual.localPosition;
			p.y = localY;
			_visual.localPosition = p;
		}

		public void AddYaw(float degPerFrame)
		{
			_visual.Rotate(0f, degPerFrame, 0f, Space.World);
		}

		public void EnableCollider(bool on) => _col.enabled = on;

		public void DestroySelf() => Destroy(gameObject);
		
		private Vector3 _baseScale;
		private void OnEnable() => _baseScale = _visual ? _visual.localScale : transform.localScale;

		public void SetScale01(float k)
		{
			if (_visual) _visual.localScale = _baseScale * Mathf.Max(0.001f, k);
			else transform.localScale = _baseScale * Mathf.Max(0.001f, k);
		}

		public void ResetScale()
		{
			if (_visual) _visual.localScale = _baseScale;
			else transform.localScale = _baseScale;
		}
	}
}