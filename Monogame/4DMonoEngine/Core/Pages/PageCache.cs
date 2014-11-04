using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Pages
{
	//TODO : refector into generic LRU cache
    public class PageCache
    {
		private readonly LinkedList<Page> m_ageQueue;
    	private readonly Dictionary<uint, LinkedListNode<Page>> m_pages;
		private readonly int m_capacity;
		private int m_loadedPages;

    	public PageCache(int capacity = 10)
    	{
			m_capacity = capacity;
			m_loadedPages = 0;
    		m_pages = new Dictionary<uint, LinkedListNode<Page>>();
			m_ageQueue = new LinkedList<Page> ();
    	}

    	public void InsertPage(Page page)
    	{
			Debug.Assert (!m_pages.ContainsKey (page.PageId), "Page already loaded into cache.");
			LinkedListNode<Page> node = null;
			if(m_loadedPages > m_capacity)
			{
				node = m_ageQueue.Last;
				DropPage (node.Value.PageId);
				node.Value = page;
			}
			else 
			{
				node = new LinkedListNode<Page> (page);
			}
			m_pages.Add(page.PageId, node);
			m_ageQueue.AddFirst(node);
    	}

    	public Page GetPage(uint pageId)
    	{
    		if(m_pages.ContainsKey(pageId))
    		{
				var node = m_pages[pageId];
				var page = node.Value;
				m_ageQueue.Remove(node);
				m_ageQueue.AddFirst(node);
				return page;
    		}
			return null;
    	}

		public bool ContainsPage(uint pageId)
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

    	public void DropPage(uint pageId)
    	{
    		if(m_pages.ContainsKey(pageId))
    		{
    			var node = m_pages[pageId];
				m_ageQueue.Remove (node);
				m_pages.Remove (pageId);
    		}
    	}
    }
}