using System.Collections.Generic;
using System.Linq;

namespace _4DMonoEngine.Core.Common.Random
{
    class DeckOfCards<T>
    {
        //TODO : since this object is stateful, it will likely need a serialize function
        protected FastRandom m_random;
        protected Stack<T> m_deck, m_discard;

        public int CardsRemaining
        {
            get { return m_deck.Count(); }
        }

        public bool AutoShuffle { get; set; }
        public int IterationsPerShuffle { get; set; }

        public DeckOfCards(IEnumerable<T> items, uint seed, int iterationsPerShuffle = 1, bool autoShuffle = true)
        {
            m_random = new FastRandom(seed);
            m_discard = new Stack<T>();
            m_deck = new Stack<T>(items);
            AutoShuffle = autoShuffle;
            IterationsPerShuffle = iterationsPerShuffle;
            if(autoShuffle)
            {
                Shuffle();
            }
        }

        public T Draw()
        {
            T ret = default(T);
            if(m_deck.Count > 0)
            {
                ret = m_deck.Pop();
            }
            else if(AutoShuffle)
            {
                Shuffle();
                ret = m_deck.Pop();
            }
            return ret;
        }

        public void Shuffle()
        {
            List<T> list = m_deck.ToList();
            while(m_discard.Count > 0)
            {
                list.Add(m_discard.Pop());
            }
            for (int i = 0; i < IterationsPerShuffle; ++i)
            {
                int n = m_deck.Count;
                while (n > 1)
                {
                    n--;
                    int k = m_random.Next(n + 1);
                    T value = list[k];
                    list[k] = list[n];
                    list[n] = value;
                }
            }
            m_deck = new Stack<T>(list);
        }
    }
}
