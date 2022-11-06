using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Bookmarks
{
	public class ObjectBookmarksEditor : EditorWindow
	{
		const string EditorPrefsKey = "handgame:BookmarksGUID";

		[MenuItem("Window/Object Bookmarks")]
		static void Init()
		{
			var window = GetWindow<ObjectBookmarksEditor>();
			window.titleContent = new GUIContent("Bookmarks");
			window.ShowUtility();
			
		}

		ObjectBookmarks bookmarks;

		List<Type> filterTypes = new List<Type>();
		string[] filterStrings = new string[] { "None" };
		int filterIndex = 0;
		bool showHistory;

		void OnEnable()
		{
			AssetDatabase.Refresh();
			ReloadBookmarksAsset();
			RefreshFilter();
			Selection.selectionChanged += OnSelectionChanged;
		}

		void OnDisable()
		{
			if (bookmarks != null)
				AssetDatabase.SaveAssetIfDirty(bookmarks);
			Selection.selectionChanged -= OnSelectionChanged;
		}

		void ReloadBookmarksAsset()
		{
			var guid = EditorPrefs.GetString(EditorPrefsKey, string.Empty);
			if (!string.IsNullOrEmpty(guid))
			{
				if (LoadBookmarksAsset(guid))
					return;
			}

			var guids = AssetDatabase.FindAssets("t:" + typeof(ObjectBookmarks));
			if (guids != null && guids.Length > 0)
			{
				guid = guids[0];

				if (LoadBookmarksAsset(guid))
					return;
			}
		}

		bool LoadBookmarksAsset(string guid)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			bookmarks = AssetDatabase.LoadAssetAtPath<ObjectBookmarks>(path);
			if (bookmarks == null)
			{
				Debug.LogWarning($"Failed to load Bookmarks at: {path}");
				return false;
			}
			return true;
		}

		void RefreshFilter()
		{
			if (bookmarks == null)
				return;

			bool changed = false;

			foreach (var obj in bookmarks.History)
				if (obj != null)
					if (!filterTypes.Contains(obj.GetType()))
					{
						filterTypes.Add(obj.GetType());
						changed = true;
					}

			if (changed)
			{
				filterStrings = new string[1 + filterTypes.Count];
				filterStrings[0] = "None";
				for (int i = 0; i < filterTypes.Count; i++)
					filterStrings[1 + i] = filterTypes[i].Name;
			}
		}

		void OnSelectionChanged()
		{
			if (bookmarks == null)
				return;

			if (Selection.count != 1)
				return;

			bookmarks.EnsureCount();

			var history = bookmarks.History;

			var obj = Selection.activeObject;

			if (obj is UnityEditor.DefaultAsset)
				return;

			//Log.D(obj.name, obj.GetType());

			bool alreadyInHistory = history.Remove(obj);

			bookmarks.History.Insert(0, obj);

			if (!alreadyInHistory)
			{
				bool deleted = false;

				if (!deleted)
					for (int i = bookmarks.History.Count - 1; i >= 0; i--)
						if (bookmarks.History[i] == null)
						{
							bookmarks.History.RemoveAt(i);
							deleted = true;
							break;
						}

				if (!deleted)
					if (filterIndex > 0)
						for (int i = bookmarks.History.Count - 1; i >= 0; i--)
							if (bookmarks.History[i].GetType() != filterTypes[filterIndex - 1])
							{
								bookmarks.History.RemoveAt(i);
								deleted = true;
								break;
							}

				if (!deleted)
					bookmarks.History.RemoveAt(bookmarks.History.Count - 1);
			}

			RefreshFilter();

			Repaint();
		}

		void PrefabStageOnprefabStageOpened(PrefabStage prefabStage)
		{
			if (bookmarks == null)
				return;

			Debug.Log("OnPrefabStageOpened " + prefabStage.assetPath);

			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabStage.assetPath);

			bookmarks.EnsureCount();

			var history = bookmarks.History;

			bool alreadyInHistory = history.Remove(prefab);

			bookmarks.History.Insert(0, prefab);

			if (!alreadyInHistory)
				bookmarks.History.RemoveAt(bookmarks.History.Count - 1);
		}

		void OnGUI()
		{
			if (bookmarks == null)
			{
				EditorGUILayout.LabelField("Missing Bookmarks Asset");
				bookmarks = (ObjectBookmarks)EditorGUILayout.ObjectField(bookmarks, typeof(ObjectBookmarks), false);

				if (bookmarks != null)
				{
					var path = AssetDatabase.GetAssetPath(bookmarks);
					var guid = AssetDatabase.AssetPathToGUID(path);

					EditorPrefs.SetString(EditorPrefsKey, guid);
					Debug.Log($"Saving Bookmarks at: {path} {guid}");
					bookmarks.EnsureCount();
				}
				else
				{
					if (GUILayout.Button("Try Reloading Asset "))
						ReloadBookmarksAsset();
				}

				return;
			}

			//Bookmarks
			{
				EditorGUI.BeginChangeCheck();

				for (int i = 0; i < bookmarks.Bookmarks.Count; i++)
				{
					GUILayout.BeginHorizontal();
					bookmarks.Bookmarks[i] = EditorGUILayout.ObjectField(bookmarks.Bookmarks[i], typeof(Object), false);
					i++;
					if (i < bookmarks.Bookmarks.Count)
						bookmarks.Bookmarks[i] = EditorGUILayout.ObjectField(bookmarks.Bookmarks[i], typeof(Object), false);
					GUILayout.EndHorizontal();
				}

				if (EditorGUI.EndChangeCheck())
					EditorUtility.SetDirty(bookmarks);
			}

			EditorGUILayout.Space();

			showHistory = EditorGUILayout.Foldout(showHistory, "History" );
			if(showHistory)
			{
				filterIndex = EditorGUILayout.Popup("Filter:", filterIndex, filterStrings);

				List<Object> history = bookmarks.History;

				if (filterIndex > 0)
				{
					history = history.Where(t => t.GetType() == filterTypes[filterIndex - 1]).ToList();
					while (history.Count < bookmarks.History.Count)
						history.Add(null);
				}

				for (int i = 0; i < history.Count; i++)
				{
					GUILayout.BeginHorizontal();
					history[i] = EditorGUILayout.ObjectField(history[i], typeof(Object), false);
					i++;
					if (i < history.Count)
						history[i] = EditorGUILayout.ObjectField(history[i], typeof(Object), false);
					GUILayout.EndHorizontal();
				}
			}

			EditorGUILayout.Space();

			if (GUILayout.Button("Change Bookmark Asset"))
				bookmarks = null;
		}
	}
}