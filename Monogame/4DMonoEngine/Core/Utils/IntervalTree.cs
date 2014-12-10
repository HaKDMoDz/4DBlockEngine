using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace _4DMonoEngine.Core.Utils
{

    /// <summary>
    /// Interval tree implementation adapted from https://brooknovak.wordpress.com/2013/12/07/augmented-interval-tree-in-c/ 
    /// Because voxel data can't overlap the original implementation was modified to no longer consider overlap to improve efficiency and reduce garbage. 
    /// This means that the implementation is decidedly less correct, but it itr works for my case.
    /// </summary>
    /// <typeparam name="TInterval">The interval type</typeparam>
    /// <typeparam name="TPoint">The interval's start and end type</typeparam>

    [Serializable]
    public class IntervalTree<TInterval, TPoint> : ICollection<TInterval>, ICollection, ISerializable
        where TPoint : IComparable<TPoint>
    {
        private readonly IIntervalSelector<TInterval, TPoint> m_intervalSelector;
        private readonly object m_syncRoot;
        private ulong m_modifications;
        private IntervalNode m_root;

        private IntervalTree()
        {
            m_syncRoot = new object();
        }

        public IntervalTree(IEnumerable<TInterval> intervals, IIntervalSelector<TInterval, TPoint> intervalSelector) :
            this(intervalSelector)
        {
            AddRange(intervals);
        }

        public IntervalTree(IIntervalSelector<TInterval, TPoint> intervalSelector)
            : this()
        {
            Debug.Assert(!ReferenceEquals(intervalSelector, null), "specified interval selector is null");
            m_intervalSelector = intervalSelector;
        }


        public IntervalTree(SerializationInfo info, StreamingContext context)
            : this()
        {
            // Reset the property value using the GetValue method.
            var intervals = (TInterval[]) info.GetValue("intervals", typeof (TInterval[]));
            m_intervalSelector =
                (IIntervalSelector<TInterval, TPoint>)
                    info.GetValue("selector", typeof (IIntervalSelector<TInterval, TPoint>));
            AddRange(intervals);
        }

        public TPoint MaxEndPoint
        {
            get
            {
                Debug.Assert(m_root != null, "Cannot determine max end point for emtpy interval tree");
                return m_root.MaxEndPoint;
            }
        }

        public TInterval this[TPoint point]
        {
            get { return FindAt(point); }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public Object SyncRoot
        {
            get { return m_syncRoot; }
        }

        public void CopyTo(
            Array array,
            int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            PerformCopy(arrayIndex, array.Length, (i, v) => array.SetValue(v, i));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new IntervalTreeEnumerator(this);
        }

        public IEnumerator<TInterval> GetEnumerator()
        {
            return new IntervalTreeEnumerator(this);
        }

        public int Count { get; private set; }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void CopyTo(
            TInterval[] array,
            int arrayIndex)
        {
            Debug.Assert(array != null, "Target array is null");
            PerformCopy(arrayIndex, array.Length, (i, v) => array[i] = v);
        }

        public bool Contains(TInterval item)
        {
            Debug.Assert(!ReferenceEquals(item, null), "specified item is null");
            return ContainsInterval(item);
        }

        public void Clear()
        {
            SetRoot(null);
            Count = 0;
            m_modifications++;
        }

        public void Add(TInterval item)
        {
            Debug.Assert(!ReferenceEquals(item, null), "specified item is null");

            var newNode = new IntervalNode(item, Start(item), End(item));

            if (m_root == null)
            {
                SetRoot(newNode);
                Count = 1;
                m_modifications++;
                return;
            }

            var node = m_root;
            while (true)
            {
                var startCmp = newNode.Start.CompareTo(node.Start);
                if (startCmp <= 0)
                {
                    if (startCmp == 0 && ReferenceEquals(node.Data, newNode.Data))
                        throw new InvalidOperationException(
                            "Cannot add the same item twice (object reference already exists in db)");

                    if (node.Left == null)
                    {
                        node.Left = newNode;
                        break;
                    }
                    node = node.Left;
                }
                else
                {
                    if (node.Right == null)
                    {
                        node.Right = newNode;
                        break;
                    }
                    node = node.Right;
                }
            }

            m_modifications++;
            Count++;

            // Restructure tree to be balanced
            node = newNode;
            while (node != null)
            {
                node.UpdateHeight();
                node.UpdateMaxEndPoint();
                Rebalance(node);
                node = node.Parent;
            }
        }

        public bool Remove(TInterval item)
        {
            Debug.Assert(!ReferenceEquals(item, null), "specified item is null");

            if (m_root == null)
            {
                return false;
            }

            var toBeRemoved = FindNode(item);

            if (toBeRemoved == null)
            {
                return false;
            }

            var parent = toBeRemoved.Parent;
            var isLeftChild = toBeRemoved.IsLeftChild;

            if (toBeRemoved.Left == null && toBeRemoved.Right == null)
            {
                if (parent != null)
                {
                    if (isLeftChild)
                        parent.Left = null;
                    else
                        parent.Right = null;

                    Rebalance(parent);
                }
                else
                {
                    SetRoot(null);
                }
            }
            else if (toBeRemoved.Right == null)
            {
                if (parent != null)
                {
                    if (isLeftChild)
                        parent.Left = toBeRemoved.Left;
                    else
                        parent.Right = toBeRemoved.Left;

                    Rebalance(parent);
                }
                else
                {
                    SetRoot(toBeRemoved.Left);
                }
            }
            else if (toBeRemoved.Left == null)
            {
                if (parent != null)
                {
                    if (isLeftChild)
                        parent.Left = toBeRemoved.Right;
                    else
                        parent.Right = toBeRemoved.Right;

                    Rebalance(parent);
                }
                else
                {
                    SetRoot(toBeRemoved.Right);
                }
            }
            else
            {
                IntervalNode replacement, replacementParent, temp;

                if (toBeRemoved.Balance > 0)
                {
                    if (toBeRemoved.Left.Right == null)
                    {
                        replacement = toBeRemoved.Left;
                        replacement.Right = toBeRemoved.Right;
                        temp = replacement;
                    }
                    else
                    {
                        replacement = toBeRemoved.Left.Right;
                        while (replacement.Right != null)
                        {
                            replacement = replacement.Right;
                        }
                        replacementParent = replacement.Parent;
                        replacementParent.Right = replacement.Left;

                        temp = replacementParent;

                        replacement.Left = toBeRemoved.Left;
                        replacement.Right = toBeRemoved.Right;
                    }
                }
                else
                {
                    if (toBeRemoved.Right.Left == null)
                    {
                        replacement = toBeRemoved.Right;
                        replacement.Left = toBeRemoved.Left;
                        temp = replacement;
                    }
                    else
                    {
                        replacement = toBeRemoved.Right.Left;
                        while (replacement.Left != null)
                        {
                            replacement = replacement.Left;
                        }
                        replacementParent = replacement.Parent;
                        replacementParent.Left = replacement.Right;

                        temp = replacementParent;

                        replacement.Left = toBeRemoved.Left;
                        replacement.Right = toBeRemoved.Right;
                    }
                }

                if (parent != null)
                {
                    if (isLeftChild)
                        parent.Left = replacement;
                    else
                        parent.Right = replacement;
                }
                else
                {
                    SetRoot(replacement);
                }

                Rebalance(temp);
            }

            toBeRemoved.Parent = null;
            Count--;
            m_modifications++;
            return true;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            var intervals = new TInterval[Count];
            CopyTo(intervals, 0);
            info.AddValue("intervals", intervals, typeof (TInterval[]));
            info.AddValue("selector", m_intervalSelector, typeof (IIntervalSelector<TInterval, TPoint>));
        }

        public void AddRange(IEnumerable<TInterval> intervals)
        {
            Debug.Assert(intervals != null, "Intervals are null");
            foreach (var interval in intervals)
            {
                Add(interval);
            }
        }

        public TInterval FindAt(TPoint point)
        {
            Debug.Assert(!ReferenceEquals(point, null), "Point is null");

            IntervalNode found;
            PerformStabbingQuery(m_root, point, out found);
            return found != null ? found.Data : default(TInterval);
        }

        public bool ContainsPoint(TPoint point)
        {
            IntervalNode found;
            PerformStabbingQuery(m_root, point, out found);
            return found != null;
        }

        public bool ContainsInterval(TInterval item)
        {
            if (ReferenceEquals(item, null))
                throw new ArgumentNullException("item");
            IntervalNode found;
            PerformStabbingQuery(m_root, item, out found);
            return found != null;
        }

        public TInterval FindInterval(TInterval item)
        {
            Debug.Assert(!ReferenceEquals(item, null), "Item is null");

            IntervalNode found;
            PerformStabbingQuery(m_root, item, out found);
            return found != null ? found.Data : default(TInterval);
        }

        private IntervalNode FindNode(TInterval item)
        {
            Debug.Assert(!ReferenceEquals(item, null), "Item is null");

            IntervalNode found;
            PerformStabbingQuery(m_root, item, out found);
            return found;
        }

        private void PerformCopy(int arrayIndex, int arrayLength, Action<int, TInterval> setAtIndexDelegate)
        {
            Debug.Assert(arrayIndex >= 0, "Specified index < 0");
            var i = arrayIndex;
            var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (i >= arrayLength)
                {
                    throw new ArgumentOutOfRangeException("arrayIndex",
                        "Not enough elements in array to copy content into");
                }
                setAtIndexDelegate(i, enumerator.Current);
                i++;
            }
        }

        private void SetRoot(IntervalNode node)
        {
            m_root = node;
            if (m_root != null)
            {
                m_root.Parent = null;
            }
        }

        private TPoint Start(TInterval interval)
        {
            return m_intervalSelector.GetStart(interval);
        }

        private TPoint End(TInterval interval)
        {
            return m_intervalSelector.GetEnd(interval);
        }

        private bool DoesIntervalContain(TInterval interval, TPoint point)
        {
            return point.CompareTo(Start(interval)) >= 0
                   && point.CompareTo(End(interval)) <= 0;
        }

        private bool DoIntervalsOverlap(TInterval interval, TInterval other)
        {
            return Start(interval).CompareTo(End(other)) <= 0 &&
                   End(interval).CompareTo(Start(other)) >= 0;
        }

        private void PerformStabbingQuery(IntervalNode node, TPoint point, out IntervalNode result)
        {
            result = null;
            while (true)
            {
                if (node == null)
                {
                    return;
                }

                if (point.CompareTo(node.MaxEndPoint) > 0)
                {
                    return;
                }

                if (node.Left != null)
                {
                    PerformStabbingQuery(node.Left, point, out result);
                    if (result != null)
                    {
                        return;
                    }
                }

                if (DoesIntervalContain(node.Data, point))
                {
                    result = node;
                    return;
                }

                if (point.CompareTo(node.Start) < 0)
                {
                    return;
                }

                if (node.Right != null)
                {
                    node = node.Right;
                    continue;
                }
                break;
            }
        }

        private IntervalNode PerformStabbingQuery(IntervalNode node, TInterval interval)
        {
            IntervalNode result;
            PerformStabbingQuery(node, interval, out result);
            return result;
        }

        private void PerformStabbingQuery(IntervalNode node, TInterval interval, out IntervalNode result)
        {
            result = null;
            while (true)
            {
                if (node == null)
                {
                    return;
                }

                if (Start(interval).CompareTo(node.MaxEndPoint) > 0)
                {
                    return;
                }

                if (node.Left != null)
                {
                    PerformStabbingQuery(node.Left, interval, out result);
                    if(result != null)
                    {
                        return;
                    }
                }

                if (DoIntervalsOverlap(node.Data, interval))
                {
                    result = node;
                    return;
                }

                if (End(interval).CompareTo(node.Start) < 0)
                {
                    return;
                }

                if (node.Right != null)
                {
                    node = node.Right;
                    continue;
                }
                break;
            }
        }

        private void Rebalance(IntervalNode node)
        {
            if (node.Balance > 1)
            {
                if (node.Left.Balance < 0)
                {
                    RotateLeft(node.Left);
                }
                RotateRight(node);
            }
            else if (node.Balance < -1)
            {
                if (node.Right.Balance > 0)
                {
                    RotateRight(node.Right);
                }
                RotateLeft(node);
            }
        }

        private void RotateLeft(IntervalNode node)
        {
            var parent = node.Parent;
            var isNodeLeftChild = node.IsLeftChild;

            // Make node.Right the new root of this sub tree (instead of node)
            var pivotNode = node.Right;
            node.Right = pivotNode.Left;
            pivotNode.Left = node;

            if (parent != null)
            {
                if (isNodeLeftChild)
                {
                    parent.Left = pivotNode;
                }
                else
                {
                    parent.Right = pivotNode;
                }
            }
            else
            {
                SetRoot(pivotNode);
            }
        }

        private void RotateRight(IntervalNode node)
        {
            var parent = node.Parent;
            var isNodeLeftChild = node.IsLeftChild;

            // Make node.Left the new root of this sub tree (instead of node)
            var pivotNode = node.Left;
            node.Left = pivotNode.Right;
            pivotNode.Right = node;

            if (parent != null)
            {
                if (isNodeLeftChild)
                {
                    parent.Left = pivotNode;
                }
                else
                {
                    parent.Right = pivotNode;
                }
            }
            else
            {
                SetRoot(pivotNode);
            }
        }

        [Serializable]
        private class IntervalNode
        {
            private IntervalNode m_left;
            private IntervalNode m_right;

            public IntervalNode(TInterval data, TPoint start, TPoint end)
            {
                if (start.CompareTo(end) > 0)
                {
                    throw new ArgumentOutOfRangeException("end",
                        "The suplied interval has an invalid range, where start is greater than end");
                }
                Data = data;
                Start = start;
                End = end;
                UpdateMaxEndPoint();
            }

            public IntervalNode Parent { get; set; }
            public TPoint Start { get; private set; }
            private TPoint End { get; set; }
            public TInterval Data { get; private set; }
            private int Height { get; set; }
            public TPoint MaxEndPoint { get; private set; }

            public IntervalNode Left
            {
                get { return m_left; }
                set
                {
                    m_left = value;
                    if (m_left != null)
                    {
                        m_left.Parent = this;
                    }
                    UpdateHeight();
                    UpdateMaxEndPoint();
                }
            }

            public IntervalNode Right
            {
                get { return m_right; }
                set
                {
                    m_right = value;
                    if (m_right != null)
                    {
                        m_right.Parent = this;
                    }
                    UpdateHeight();
                    UpdateMaxEndPoint();
                }
            }

            public int Balance
            {
                get
                {
                    if (Left != null && Right != null)
                    {
                        return Left.Height - Right.Height;
                    }
                    if (Left != null)
                    {
                        return Left.Height + 1;
                    }
                    if (Right != null)
                    {
                        return -(Right.Height + 1);
                    }
                    return 0;
                }
            }

            public bool IsLeftChild
            {
                get { return Parent != null && Parent.Left == this; }
            }

            public void UpdateHeight()
            {
                if (Left != null && Right != null)
                {
                    Height = Math.Max(Left.Height, Right.Height) + 1;
                }
                else if (Left != null)
                {
                    Height = Left.Height + 1;
                }
                else if (Right != null)
                {
                    Height = Right.Height + 1;
                }
                else
                {
                    Height = 0;
                }
            }

            private static TPoint Max(TPoint comp1, TPoint comp2)
            {
                return comp1.CompareTo(comp2) > 0 ? comp1 : comp2;
            }

            public void UpdateMaxEndPoint()
            {
                var max = End;
                if (Left != null)
                {
                    max = Max(max, Left.MaxEndPoint);
                }
                if (Right != null)
                {
                    max = Max(max, Right.MaxEndPoint);
                }

            MaxEndPoint = max;
            }

            public override string ToString()
            {
                return string.Format("[{0},{1}], maxEnd={2}", Start, End, MaxEndPoint);
            }
        }

        private class IntervalTreeEnumerator : IEnumerator<TInterval>
        {
            private readonly ulong m_modificationsAtCreation;
            private readonly IntervalNode m_startNode;
            private readonly IntervalTree<TInterval, TPoint> m_tree;
            private IntervalNode m_current;
            private bool m_hasVisitedCurrent;
            private bool m_hasVisitedRight;

            public IntervalTreeEnumerator(IntervalTree<TInterval, TPoint> tree)
            {
                m_tree = tree;
                m_modificationsAtCreation = tree.m_modifications;
                m_startNode = GetLeftMostDescendantOrSelf(tree.m_root);
                Reset();
            }

            public TInterval Current
            {
                get
                {
                    if (m_current == null)
                    {
                        throw new InvalidOperationException("Enumeration has finished.");
                    }

                    if (ReferenceEquals(m_current, m_startNode) && !m_hasVisitedCurrent)
                    {
                        throw new InvalidOperationException("Enumeration has not started.");
                    }

                    return m_current.Data;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Reset()
            {
                if (m_modificationsAtCreation != m_tree.m_modifications)
                {
                    throw new InvalidOperationException("Collection was modified.");
                }
                m_current = m_startNode;
                m_hasVisitedCurrent = false;
                m_hasVisitedRight = false;
            }

            public bool MoveNext()
            {
                if (m_modificationsAtCreation != m_tree.m_modifications)
                {
                    throw new InvalidOperationException("Collection was modified.");
                }

                if (m_tree.m_root == null)
                {
                    return false;
                }

                // Visit this node
                if (!m_hasVisitedCurrent)
                {
                    m_hasVisitedCurrent = true;
                    return true;
                }

                // Go right, visit the right's left most descendant (or the right node itself)
                if (!m_hasVisitedRight && m_current.Right != null)
                {
                    m_current = m_current.Right;
                    MoveToLeftMostDescendant();
                    m_hasVisitedCurrent = true;
                    m_hasVisitedRight = false;
                    return true;
                }

                // Move upward
                do
                {
                    var wasVisitingFromLeft = m_current.IsLeftChild;
                    m_current = m_current.Parent;
                    if (!wasVisitingFromLeft)
                    {
                        continue;
                    }
                    m_hasVisitedCurrent = false;
                    m_hasVisitedRight = false;
                    return MoveNext();
                } while (m_current != null);

                return false;
            }

            public void Dispose()
            {
            }

            private void MoveToLeftMostDescendant()
            {
                m_current = GetLeftMostDescendantOrSelf(m_current);
            }

            private static IntervalNode GetLeftMostDescendantOrSelf(IntervalNode node)
            {
                if (node == null)
                {
                    return null;
                }
                while (node.Left != null)
                {
                    node = node.Left;
                }
                return node;
            }
        }
    }

    public interface IIntervalSelector<in TInterval, out TPoint> where TPoint : IComparable<TPoint>
    {
        TPoint GetStart(TInterval item);
        TPoint GetEnd(TInterval item);
    }
}