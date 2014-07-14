using System;
using System.Collections.Generic;

namespace _4DMonoEngine.Core.Utils.Random
{
    class BallInBin<T>
    {
        //TODO : since this object is stateful, it will likely need a serialize function
        private readonly FastRandom m_random;
        private readonly List<T> m_bins;
        private Dictionary<T, float> m_probabilities;
        private readonly Func<T, float> m_probabilityFunction;

        public BallInBin(IEnumerable<T> items, Func<T, float> probabilityFunction, uint seed)
        {
            m_random = new FastRandom(seed);
            m_bins = new List<T>(items);
            m_probabilityFunction = probabilityFunction;
            UpdateProbabilities();
        }

        public T Toss()
        {
            var ret = default(T);
            var randomRoll = m_random.NextDouble();
            double cumulativePercentages = 0;
            foreach (var element in m_bins)
            {
                cumulativePercentages += m_probabilities[element];
                if (!(cumulativePercentages >= randomRoll))
                {
                    continue;
                }
                ret = element;
                break;
            }
            return ret;
        }

        public void UpdateProbabilities()
        {
            m_probabilities = new Dictionary<T, float>();
            float total = 0;
            foreach (var element in m_bins)
            {
                var prob = m_probabilityFunction(element);
                total += prob;
                m_probabilities.Add(element, prob);
            }
            foreach (var element in m_bins)
            {
                m_probabilities[element] = m_probabilities[element] / total;
            }
            m_bins.Sort((x, y) => (int)(m_probabilities[x] - m_probabilities[y]));            
        }
    }
}
