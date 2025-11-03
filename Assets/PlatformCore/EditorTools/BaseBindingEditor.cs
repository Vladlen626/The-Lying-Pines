#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using System;
using System.Collections.Generic;
using PlatformCore.Services.UI;
using PlatformCore.Services.UI.Bindings;
using PlatformCore.UIFramework.Runtime.Bindables;

namespace PlatformCore.UIFramework.Runtime.Bindings.Editor
{
	// Атрибут для маркировки биндингов
	public class BindingTargetAttribute : Attribute
	{
		public Type TargetType { get; }

		public BindingTargetAttribute(Type targetType)
		{
			TargetType = targetType;
		}
	}

	// Универсальный редактор для всех биндингов
	[CustomEditor(typeof(MonoBehaviour), true)]
	public class UniversalBindingEditor : UnityEditor.Editor
	{
		private SerializedProperty _targetScreen;
		private SerializedProperty _propertyName;
		private List<SerializedProperty> _otherProperties = new();

		private bool _isBindingComponent = false;
		private Type _bindableType;

		private void OnEnable()
		{
			// Проверяем, является ли это биндинг-компонентом
			_isBindingComponent = CheckIfBindingComponent();

			if (!_isBindingComponent) return;

			// Находим стандартные поля
			_targetScreen = serializedObject.FindProperty("_targetScreen");
			_propertyName = serializedObject.FindProperty("_propertyName");

			// Собираем все остальные поля
			CollectOtherProperties();
		}

		private bool CheckIfBindingComponent()
		{
			var targetType = target.GetType();

			// Ищем атрибут BindingTarget
			var attribute = targetType.GetCustomAttribute<BindingTargetAttribute>();
			if (attribute != null)
			{
				_bindableType = attribute.TargetType;
				return true;
			}

			// Альтернативно: проверяем по наличию полей _targetScreen и _propertyName
			var hasTargetScreen =
				targetType.GetField("_targetScreen", BindingFlags.NonPublic | BindingFlags.Instance) != null;
			var hasPropertyName =
				targetType.GetField("_propertyName", BindingFlags.NonPublic | BindingFlags.Instance) != null;

			if (hasTargetScreen && hasPropertyName)
			{
				// Пытаемся угадать тип по названию класса
				_bindableType = GuessBindableTypeFromClassName(targetType.Name);
				return _bindableType != null;
			}

			return false;
		}

		private Type GuessBindableTypeFromClassName(string className)
		{
			if (className.StartsWith("Bool")) return typeof(BindableBool);
			if (className.StartsWith("Float")) return typeof(BindableFloat);
			if (className.StartsWith("Int")) return typeof(BindableInt);
			if (className.StartsWith("String")) return typeof(BindableString);

			// Поиск по содержимому названия
			if (className.Contains("Bool")) return typeof(BindableBool);
			if (className.Contains("Float")) return typeof(BindableFloat);
			if (className.Contains("Int")) return typeof(BindableInt);
			if (className.Contains("String")) return typeof(BindableString);

			return null;
		}

		private void CollectOtherProperties()
		{
			_otherProperties.Clear();

			var iterator = serializedObject.GetIterator();
			iterator.NextVisible(true);

			do
			{
				// Пропускаем стандартные Unity поля и наши служебные
				if (iterator.propertyPath == "m_Script" ||
				    iterator.propertyPath == "_targetScreen" ||
				    iterator.propertyPath == "_propertyName")
					continue;

				_otherProperties.Add(serializedObject.FindProperty(iterator.propertyPath));
			} while (iterator.NextVisible(false));
		}

		public override void OnInspectorGUI()
		{
			if (!_isBindingComponent)
			{
				// Если это не биндинг-компонент, используем стандартный Editor
				DrawDefaultInspector();
				return;
			}

			serializedObject.Update();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Binding Configuration", EditorStyles.boldLabel);

			// Target Screen
			EditorGUILayout.PropertyField(_targetScreen, new GUIContent("Target Screen"));

			// Property Name Dropdown  
			DrawPropertyNameDropdown();

			// Все остальные свойства
			if (_otherProperties.Count > 0)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

				foreach (var prop in _otherProperties)
				{
					if (prop != null)
						EditorGUILayout.PropertyField(prop);
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawPropertyNameDropdown()
		{
			if (_targetScreen.objectReferenceValue != null && _bindableType != null)
			{
				var targetScreen = _targetScreen.objectReferenceValue as BaseUIElement;
				var properties = GetBindableProperties(targetScreen, _bindableType);

				if (properties.Length > 0)
				{
					var currentIndex = Array.IndexOf(properties, _propertyName.stringValue);
					if (currentIndex < 0) currentIndex = 0;

					var newIndex = EditorGUILayout.Popup("Property Name", currentIndex, properties);
					if (newIndex >= 0 && newIndex < properties.Length)
					{
						_propertyName.stringValue = properties[newIndex];
					}
				}
				else
				{
					EditorGUILayout.LabelField("Property Name", $"No {_bindableType.Name} properties found");
				}
			}
			else
			{
				EditorGUILayout.LabelField("Property Name", "Select Target Screen first");
			}
		}

		private string[] GetBindableProperties(BaseUIElement screen, Type bindableType)
		{
			if (screen == null) return new string[0];

			var fields = screen.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(f => f.FieldType == bindableType)
				.Select(f => f.Name)
				.ToArray();

			return fields;
		}
	}
}
#endif