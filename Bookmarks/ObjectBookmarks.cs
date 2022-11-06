using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Bookmarks
{
	[CreateAssetMenu(fileName = "SavedBookmarks", menuName = "Devm/SavedBookmarks", order = 0)]
	public class ObjectBookmarks : ScriptableObject
	{
		[Range(1, 32)]
		public int BookmarksCount = 6;

		[Range(1, 32)]
		public int HistoryCount = 6;

		public List<Object> Bookmarks = new List<Object>(6);
		public List<Object> History = new List<Object>(6);

		public void EnsureCount()
		{
			ResizeList(Bookmarks, BookmarksCount);
			ResizeList(History, HistoryCount);
		}

		void ResizeList(List<Object> list, int count)
		{
			while (list.Count > count)
			{
				list.RemoveAt(list.Count-1);
			}
			while (list.Count < count)
			{
				list.Add(null);
			}
		}
	}
}