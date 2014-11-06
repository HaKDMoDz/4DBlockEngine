using System.Collections.Generic;
using System.Diagnostics;
using _4DMonoEngine.Core.Common.Interfaces;
using _4DMonoEngine.Core.Pages;

namespace _4DMonoEngine.Core.Utils
{
	//TODO : refector into generic LRU cache
    public class LruCache<T> where T : IPageable
    {
		private readonly LinkedList<T> m_ageQueue;
    	private readonly Dictionary<int, LinkedListNode<T>> m_pages;
		private readonly int m_capacity;
		private int m_loadedPages;

    	public LruCache(int capacity = 10)
    	{
			m_capacity = capacity;
			m_loadedPages = 0;
    		m_pages = new Dictionary<int, LinkedListNode<T>>();
			m_ageQueue = new LinkedList<T> ();
    	}

    	public void InsertPage(T page)
    	{
			Debug.Assert (!m_pages.ContainsKey (page.PageId), "Page already loaded into cache.");
			LinkedListNode<T> node;
			if(m_loadedPages > m_capacity)
			{
				node = m_ageQueue.Last;
				DropPage (node.Value.PageId);
				node.Value = page;
			}
			else 
			{
				node = new LinkedListNode<T> (page);
			}
			m_pages.Add(page.PageId, node);
			m_ageQueue.AddFirst(node);
    	}

    	public T GetPage(int pageId)
    	{
    	    if (!m_pages.ContainsKey(pageId))
    	    {
    	        return default(T);
    	    }
    	    var node = m_pages[pageId];
    	    var page = node.Value;
    	    m_ageQueue.Remove(node);
    	    m_ageQueue.AddFirst(node);
    	    return page;
    	}

		public bool ContainsPage(int pageId)
		{
			if(m_pages.ContainsKey(pageId))
			{
				var node = m_pages[pageId];
				m_ageQueue.Remove(node);
				m_ageQueue.AddFirst(node);
				return true;
			}
			return false;
		}

		public void EvictCache()
		{
			m_pages.Clear();
			m_ageQueue.Clear ();
			m_loadedPages = 0;
		}

    	public void DropPage(int pageId)
    	{
    	    if (!m_pages.ContainsKey(pageId))
    	    {
    	        return;
    	    }
    	    var node = m_pages[pageId];
    	    m_ageQueue.Remove (node);
    	    m_pages.Remove (pageId);
    	}
    }
}