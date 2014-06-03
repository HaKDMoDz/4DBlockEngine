using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _4DMonoEngine.Core.Common.Random
{
    class BallInBin<T>
    {
        //TODO : since this object is stateful, it will likely need a serialize function
        protected FastRandom m_random;
        protected List<T> m_bins;
        protected Dictionary<T, float> m_probabilities;
        protected Func<T, float> m_probabilityFunction;

        public BallInBin(IEnumerable<T> items, Func<T, float> probabilityFunction, int seed)
        {
            m_random = new FastRandom(seed);
            m_bins = new List<T>(items);
            m_probabilityFunction = probabilityFunction;

            UpdateProbabilities();
        }

        public T Toss()
        {
            T ret = default(T);
            double randomRoll = m_random.NextDouble();
            double cumulativePercentages = 0;
            foreach (T element in m_bins)
            {
                cumulativePercentages += m_probabilities[element];
                if (cumulativePercentages >= randomRoll)
                {
                    ret = element;
                    break;
                }
            }
            return ret;
        }

        public void UpdateProbabilities()
        {
            Dictionary<T, float> probabilities = new Dictionary<T, float>();
            float total = 0;
            foreach (T element in m_bins)
            {
                float prob = m_probabilityFunction(element);
                total += prob;
                probabilities.Add(element, prob);
            }
            foreach (T element in m_bins)
            {
                probabilities[element] = probabilities[element] / total;
            }
            m_bins.Sort((x, y) => (int)(probabilities[x] - probabilities[y]));            
        }
    }
}
