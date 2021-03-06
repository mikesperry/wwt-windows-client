//------------------------------------------------------------------------------
// Abstract implementation of a Red-Black tree KeyValuePair enumerator
//
// <copyright file="TreeDictionaryKeyValuePairEnumerator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// File History:
//           paulkoch        June 19, 2007        created
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MicrosoftInternal.AdvancedCollections
{
    public sealed class TreeDictionaryKeyValuePairEnumerator<TKey, TValue> : System.Collections.IDictionaryEnumerator, IEnumerator<KeyValuePair<TKey, TValue>>, IEquatable<TreeDictionaryKeyValuePairEnumerator<TKey, TValue>>, IComparable<TreeDictionaryKeyValuePairEnumerator<TKey, TValue>>, IEquatable<TreeDictionaryKeyEnumerator<TKey, TValue>>, IComparable<TreeDictionaryKeyEnumerator<TKey, TValue>>, IEquatable<TreeDictionaryValueEnumerator<TKey, TValue>>, IComparable<TreeDictionaryValueEnumerator<TKey, TValue>>
    {
        internal const uint IsStartingEnumeration = 0x80000000;
        internal const uint IsBeforeLowestBit = 0x40000000;
        internal const uint IsAfterHighestBit = 0x20000000;

        internal IComparer<TKey> comparer;
        internal TreeDictionary<TKey, TValue>.TreeNode loopbackNode;
        internal TreeDictionary<TKey, TValue>.TreeNode node;
        internal bool isGoingDown;
        internal uint statusBits;

        public TreeDictionaryKeyValuePairEnumerator(TreeDictionaryKeyValuePairEnumerator<TKey, TValue> enumerator)
        {
            this.comparer = enumerator.comparer;
            this.loopbackNode = enumerator.loopbackNode;
            this.node = enumerator.node;
            this.isGoingDown = enumerator.isGoingDown;
            this.statusBits = enumerator.statusBits;
        }

        public TreeDictionaryKeyValuePairEnumerator(TreeDictionaryKeyEnumerator<TKey, TValue> enumerator)
        {
            this.comparer = enumerator.comparer;
            this.loopbackNode = enumerator.loopbackNode;
            this.node = enumerator.node;
            this.isGoingDown = enumerator.isGoingDown;
            this.statusBits = enumerator.statusBits;
        }

        public TreeDictionaryKeyValuePairEnumerator(TreeDictionaryValueEnumerator<TKey, TValue> enumerator)
        {
            this.comparer = enumerator.comparer;
            this.loopbackNode = enumerator.loopbackNode;
            this.node = enumerator.node;
            this.isGoingDown = enumerator.isGoingDown;
            this.statusBits = enumerator.statusBits;
        }

        internal TreeDictionaryKeyValuePairEnumerator(TreeDictionary<TKey, TValue> tree, TreeDictionary<TKey, TValue>.TreeNode node, bool isGoingDown, uint statusBits)
        {
            this.comparer = tree.Comparer;
            this.loopbackNode = tree.loopbackNode;
            this.node = node;
            this.isGoingDown = isGoingDown;
            this.statusBits = statusBits;
        }

        public KeyValuePair<TKey, TValue> Current
        {
            get
            {
                if(statusBits != 0)
                {
                    throw new InvalidOperationException("TreeDictionaryKeyValuePairEnumerator<TKey, TValue>.Current is not positioned on a valid item");
                }
                return new KeyValuePair<TKey, TValue>(node.Key, node.Value);
            }
        }

        public TraversalDirection Direction
        {
            get
            {
                return !isGoingDown ? TraversalDirection.LowToHigh : TraversalDirection.HighToLow;
            }
            set
            {
                if(value == TraversalDirection.LowToHigh)
                {
                    isGoingDown = false;
                }
                else if(value == TraversalDirection.HighToLow)
                {
                    isGoingDown = true;
                }
                else
                {
                    throw new ArgumentException("Direction must either be TraversalDirection.LowToHigh or TraversalDirection.HighToLow", "value");
                }
            }
        }

        public bool IsAfterHighest
        {
            get
            {
                return (statusBits & IsAfterHighestBit) != 0;
            }
        }

        public bool IsBeforeLowest
        {
            get
            {
                return (statusBits & IsBeforeLowestBit) != 0;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502", Justification = "Complex for speed")]
        public int CompareTo(TreeDictionaryKeyValuePairEnumerator<TKey, TValue> other)
        {
            // if this node has been set to null, then it has been deleted
            // if the loopbackNode has been set to red (which is an invalid state in the red-black tree, then the entire tree
            // has been deleted via the Clear() function
            if(this.node.Parent == null || this.loopbackNode.IsRed || other.node.Parent == null || other.loopbackNode.IsRed)
            {
                throw new InvalidOperationException("This TreeDictionary node has been deleted");
            }

            if(this.loopbackNode != other.loopbackNode)
            {
                throw new ArgumentException("Both enumerators must be created from the same TreeDictionary", "other");
            }

            if((this.statusBits & IsBeforeLowestBit) != 0)
            {
                if((other.statusBits & IsBeforeLowestBit) != 0)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else if((other.statusBits & IsBeforeLowestBit) != 0)
            {
                return 1;
            }

            if((this.statusBits & IsAfterHighestBit) != 0)
            {
                if((other.statusBits & IsAfterHighestBit) != 0)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            else if((other.statusBits & IsAfterHighestBit) != 0)
            {
                return -1;
            }

            Debug.Assert((this.statusBits & IsStartingEnumeration) == 0 && (other.statusBits & IsStartingEnumeration) == 0);
            Debug.Assert(this.node != this.loopbackNode && this.node != other.loopbackNode && other.node != this.loopbackNode && other.node != other.loopbackNode);

            if(this.node == other.node)
            {
                return 0;
            }

            int thisHeight = comparer.Compare(this.node.Key, other.node.Key);

            if(thisHeight != 0)
            {
                return thisHeight;
            }

            thisHeight = 0;
            TreeDictionary<TKey, TValue>.TreeNode node1 = this.node.Parent;
            TreeDictionary<TKey, TValue>.TreeNode node2 = this.loopbackNode;

            while(node1 != node2)
            {
                Debug.Assert(node1 != this.loopbackNode && node1 != other.loopbackNode);
                ++thisHeight;
                node1 = node1.Parent;
            }

            int otherHeight = 0;
            node1 = other.node.Parent;
            while(node1 != node2)
            {
                Debug.Assert(node1 != this.loopbackNode && node1 != other.loopbackNode);
                ++otherHeight;
                node1 = node1.Parent;
            }

            node1 = this.node;
            node2 = other.node;
            int diff = thisHeight - otherHeight;

            TreeDictionary<TKey, TValue>.TreeNode temp1 = null;
            TreeDictionary<TKey, TValue>.TreeNode temp2 = null;

            if(otherHeight < thisHeight)
            {
                while(diff > 0)
                {
                    Debug.Assert(node1 != this.loopbackNode && node1 != other.loopbackNode);
                    temp1 = node1;
                    node1 = node1.Parent;
                    --diff;
                }
            }
            else
            {
                while(diff < 0)
                {
                    Debug.Assert(node2 != this.loopbackNode && node2 != other.loopbackNode);
                    temp2 = node2;
                    node2 = node2.Parent;
                    ++diff;
                }
            }

            while(node1 != node2)
            {
                Debug.Assert(node1 != this.loopbackNode && node1 != other.loopbackNode && node2 != this.loopbackNode && node2 != other.loopbackNode);

                temp1 = node1;
                temp2 = node2;
                node1 = node1.Parent;
                node2 = node2.Parent;
            }

            node1 = node1.Left;
            if(temp1 == null)
            {
                if(temp2 == node1)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if(temp1 == node1)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502", Justification = "Complex for speed")]
        public int CompareTo(TreeDictionaryKeyEnumerator<TKey, TValue> other)
        {
            // if this node has been set to null, then it has been deleted
            // if the loopbackNode has been set to red (which is an invalid state in the red-black tree, then the entire tree
            // has been deleted via the Clear() function
            if(this.node.Parent == null || this.loopbackNode.IsRed || other.node.Parent == null || other.loopbackNode.IsRed)
            {
                throw new InvalidOperationException("This TreeDictionary node has been deleted");
            }

            if(this.loopbackNode != other.loopbackNode)
            {
                throw new ArgumentException("Both enumerators must be created from the same TreeDictionary", "other");
            }

            if((this.statusBits & IsBeforeLowestBit) != 0)
            {
                if((other.statusBits & IsBeforeLowestBit) != 0)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else if((other.statusBits & IsBeforeLowestBit) != 0)
            {
                return 1;
            }

            if((this.statusBits & IsAfterHighestBit) != 0)
            {
                if((other.statusBits & IsAfterHighestBit) != 0)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            else if((other.statusBits & IsAfterHighestBit) != 0)
            {
                return -1;
            }

            Debug.Assert((this.statusBits & IsStartingEnumeration) == 0 && (other.statusBits & IsStartingEnumeration) == 0);
            Debug.Assert(this.node != this.loopbackNode && this.node != other.loopbackNode && other.node != this.loopbackNode && other.node != other.loopbackNode);

            if(this.node == other.node)
            {
                return 0;
            }

            int thisHeight = comparer.Compare(this.node.Key, other.node.Key);

            if(thisHeight != 0)
            {
                return thisHeight;
            }

            thisHeight = 0;
            TreeDictionary<TKey, TValue>.TreeNode node1 = this.node.Parent;
            TreeDictionary<TKey, TValue>.TreeNode node2 = this.loopbackNode;

            while(node1 != node2)
            {
                Debug.Assert(node1 != this.loopbackNode && node1 != other.loopbackNode);
                ++thisHeight;
                node1 = node1.Parent;
            }

            int otherHeight = 0;
            node1 = other.node.Parent;
            while(node1 != node2)
            {
                Debug.Assert(node1 != this.loopbackNode && node1 != other.loopbackNode);
                ++otherHeight;
                node1 = node1.Parent;
            }

            node1 = this.node;
            node2 = other.node;
            int diff = thisHeight - otherHeight;

            TreeDictionary<TKey, TValue>.TreeNode temp1 = null;
            TreeDictionary<TKey, TValue>.TreeNode temp2 = null;

            if(otherHeight < thisHeight)
            {
                while(diff > 0)
                {
                    Debug.Assert(node1 != this.loopbackNode && node1 != other.loopbackNode);
                    temp1 = node1;
                    node1 = node1.Parent;
                    --diff;
                }
            }
            else
            {
                while(diff < 0)
                {
                    Debug.Assert(node2 != this.loopbackNode && node2 != other.loopbackNode);
                    temp2 = node2;
                    node2 = node2.Parent;
                    ++diff;
                }
            }

            while(node1 != node2)
            {
                Debug.Assert(node1 != this.loopbackNode && node1 != other.loopbackNode && node2 != this.loopbackNode && node2 != other.loopbackNode);

                temp1 = node1;
                temp2 = node2;
                node1 = node1.Parent;
                node2 = node2.Parent;
            }

            node1 = node1.Left;
            if(temp1 == null)
            {
                if(temp2 == node1)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if(temp1 == node1)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502", Justification = "Complex for speed")]
        public int CompareTo(TreeDictionaryValueEnumerator<TKey, TValue> other)
        {
            // if this node has been set to null, then it has been deleted
            // if the loopbackNode has been set to red (which is an invalid state in the red-black tree, then the entire tree
            // has been deleted via the Clear() function
            if(this.node.Parent == null || this.loopbackNode.IsRed || other.node.Parent == null || other.loopbackNode.IsRed)
            {
                throw new InvalidOperationException("This TreeDictionary node has been deleted");
            }

            if(this.loopbackNode != other.loopbackNode)
            {
                throw new ArgumentException("Both enumerators must be created from the same TreeDictionary", "other");
            }

            if((this.statusBits & IsBeforeLowestBit) != 0)
            {
                if((other.statusBits & IsBeforeLowestBit) != 0)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else if((other.statusBits & IsBeforeLowestBit) != 0)
            {
                return 1;
            }

            if((this.statusBits & IsAfterHighestBit) != 0)
            {
                if((other.statusBits & IsAfterHighestBit) != 0)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            else if((other.statusBits & IsAfterHighestBit) != 0)
            {
                return -1;
            }

            Debug.Assert((this.statusBits & IsStartingEnumeration) == 0 && (other.statusBits & IsStartingEnumeration) == 0);
            Debug.Assert(this.node != this.loopbackNode && this.node != other.loopbackNode && other.node != this.loopbackNode && other.node != other.loopbackNode);

            if(this.node == other.node)
            {
                return 0;
            }

            int thisHeight = comparer.Compare(this.node.Key, other.node.Key);

            if(thisHeight != 0)
            {
                return thisHeight;
            }

            thisHeight = 0;
            TreeDictionary<TKey, TValue>.TreeNode node1 = this.node.Parent;
            TreeDictionary<TKey, TValue>.TreeNode node2 = this.loopbackNode;

            while(node1 != node2)
            {
                Debug.Assert(node1 != this.loopbackNode && node1 != other.loopbackNode);
                ++thisHeight;
                node1 = node1.Parent;
            }

            int otherHeight = 0;
            node1 = other.node.Parent;
            while(node1 != node2)
            {
                Debug.Assert(node1 != this.loopbackNode && node1 != other.loopbackNode);
                ++otherHeight;
                node1 = node1.Parent;
            }

            node1 = this.node;
            node2 = other.node;
            int diff = thisHeight - otherHeight;

            TreeDictionary<TKey, TValue>.TreeNode temp1 = null;
            TreeDictionary<TKey, TValue>.TreeNode temp2 = null;

            if(otherHeight < thisHeight)
            {
                while(diff > 0)
                {
                    Debug.Assert(node1 != this.loopbackNode && node1 != other.loopbackNode);
                    temp1 = node1;
                    node1 = node1.Parent;
                    --diff;
                }
            }
            else
            {
                while(diff < 0)
                {
                    Debug.Assert(node2 != this.loopbackNode && node2 != other.loopbackNode);
                    temp2 = node2;
                    node2 = node2.Parent;
                    ++diff;
                }
            }

            while(node1 != node2)
            {
                Debug.Assert(node1 != this.loopbackNode && node1 != other.loopbackNode && node2 != this.loopbackNode && node2 != other.loopbackNode);

                temp1 = node1;
                temp2 = node2;
                node1 = node1.Parent;
                node2 = node2.Parent;
            }

            node1 = node1.Left;
            if(temp1 == null)
            {
                if(temp2 == node1)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if(temp1 == node1)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }

        public override bool Equals(object obj)
        {
            if(obj == null)
            {
                return false;
            }

            TreeDictionaryKeyValuePairEnumerator<TKey, TValue> otherKeyValuePair = obj as TreeDictionaryKeyValuePairEnumerator<TKey, TValue>;
            if(otherKeyValuePair != null)
                return this.Equals(otherKeyValuePair);

            TreeDictionaryKeyEnumerator<TKey, TValue> otherKey = obj as TreeDictionaryKeyEnumerator<TKey, TValue>;
            if(otherKey != null)
                return this.Equals(otherKey);

            TreeDictionaryValueEnumerator<TKey, TValue> otherValue = obj as TreeDictionaryValueEnumerator<TKey, TValue>;
            if(otherValue != null)
                return this.Equals(otherValue);

            return false;
        }

        public bool Equals(TreeDictionaryKeyValuePairEnumerator<TKey, TValue> other)
        {
            // if this node has been set to null, then it has been deleted
            // if the loopbackNode has been set to red (which is an invalid state in the red-black tree, then the entire tree
            // has been deleted via the Clear() function
            if(this.node.Parent == null || this.loopbackNode.IsRed || other.node.Parent == null || other.loopbackNode.IsRed)
            {
                throw new InvalidOperationException("This TreeDictionary node has been deleted");
            }

            if(this.loopbackNode != other.loopbackNode)
            {
                throw new ArgumentException("Both enumerators must be created from the same TreeDictionary", "other");
            }

            if((this.statusBits & IsBeforeLowestBit) != 0)
            {
                if((other.statusBits & IsBeforeLowestBit) != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if((this.statusBits & IsBeforeLowestBit) != 0)
            {
                return false;
            }

            if((this.statusBits & IsAfterHighestBit) != 0)
            {
                if((other.statusBits & IsAfterHighestBit) != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if((this.statusBits & IsAfterHighestBit) != 0)
            {
                return false;
            }

            Debug.Assert((this.statusBits & IsStartingEnumeration) == 0 && (other.statusBits & IsStartingEnumeration) == 0);

            if(this.node != other.node)
                return false;

            return true;
        }

        public bool Equals(TreeDictionaryKeyEnumerator<TKey, TValue> other)
        {
            // if this node has been set to null, then it has been deleted
            // if the loopbackNode has been set to red (which is an invalid state in the red-black tree, then the entire tree
            // has been deleted via the Clear() function
            if(this.node.Parent == null || this.loopbackNode.IsRed || other.node.Parent == null || other.loopbackNode.IsRed)
            {
                throw new InvalidOperationException("This TreeDictionary node has been deleted");
            }

            if(this.loopbackNode != other.loopbackNode)
            {
                throw new ArgumentException("Both enumerators must be created from the same TreeDictionary", "other");
            }

            if((this.statusBits & IsBeforeLowestBit) != 0)
            {
                if((other.statusBits & IsBeforeLowestBit) != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if((this.statusBits & IsBeforeLowestBit) != 0)
            {
                return false;
            }

            if((this.statusBits & IsAfterHighestBit) != 0)
            {
                if((other.statusBits & IsAfterHighestBit) != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if((this.statusBits & IsAfterHighestBit) != 0)
            {
                return false;
            }

            Debug.Assert((this.statusBits & IsStartingEnumeration) == 0 && (other.statusBits & IsStartingEnumeration) == 0);

            if(this.node != other.node)
                return false;

            return true;
        }

        public bool Equals(TreeDictionaryValueEnumerator<TKey, TValue> other)
        {
            // if this node has been set to null, then it has been deleted
            // if the loopbackNode has been set to red (which is an invalid state in the red-black tree, then the entire tree
            // has been deleted via the Clear() function
            if(this.node.Parent == null || this.loopbackNode.IsRed || other.node.Parent == null || other.loopbackNode.IsRed)
            {
                throw new InvalidOperationException("This TreeDictionary node has been deleted");
            }

            if(this.loopbackNode != other.loopbackNode)
            {
                throw new ArgumentException("Both enumerators must be created from the same TreeDictionary", "other");
            }

            if((this.statusBits & IsBeforeLowestBit) != 0)
            {
                if((other.statusBits & IsBeforeLowestBit) != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if((this.statusBits & IsBeforeLowestBit) != 0)
            {
                return false;
            }

            if((this.statusBits & IsAfterHighestBit) != 0)
            {
                if((other.statusBits & IsAfterHighestBit) != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if((this.statusBits & IsAfterHighestBit) != 0)
            {
                return false;
            }

            Debug.Assert((this.statusBits & IsStartingEnumeration) == 0 && (other.statusBits & IsStartingEnumeration) == 0);

            if(this.node != other.node)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            if(statusBits == 0)
            {
                return node.Key.GetHashCode();
            }
            else
            {
                return (int)(statusBits & (~IsStartingEnumeration));
            }
        }

        public bool MoveNext()
        {
            TreeDictionary<TKey, TValue>.TreeNode currentNode = node;
            TreeDictionary<TKey, TValue>.TreeNode localLoopbackNode = loopbackNode;
            TreeDictionary<TKey, TValue>.TreeNode nextNode;

            // if this node has been set to null, then it has been deleted
            // if the loopbackNode has been set to red (which is an invalid state in the red-black tree, then the entire tree
            // has been deleted via the Clear() function
            if(currentNode.Parent == null || localLoopbackNode.IsRed)
            {
                throw new InvalidOperationException("This TreeDictionary node has been deleted");
            }

            if(statusBits == 0)
            {
                Debug.Assert(node != null);
                Debug.Assert(node != localLoopbackNode);
                Debug.Assert((statusBits & IsBeforeLowestBit) == 0);
                Debug.Assert((statusBits & IsAfterHighestBit) == 0);
                if(!isGoingDown)
                {
                    nextNode = currentNode.Right;
                    if(nextNode == localLoopbackNode)
                    {
                        if(currentNode != localLoopbackNode.Right)
                        {
                            while(true)
                            {
                                nextNode = currentNode.Parent;
                                if(nextNode.Left != currentNode)
                                {
                                    currentNode = nextNode;
                                    continue;
                                }
                                else
                                {
                                    node = nextNode;
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            statusBits = IsAfterHighestBit;
                            return false;
                        }
                    }
                    else
                    {
                        currentNode = nextNode;
                        while(true)
                        {
                            nextNode = currentNode.Left;
                            if(nextNode != localLoopbackNode)
                            {
                                currentNode = nextNode;
                                continue;
                            }
                            else
                            {
                                node = currentNode;
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    nextNode = currentNode.Left;
                    if(nextNode == localLoopbackNode)
                    {
                        if(currentNode != localLoopbackNode.Left)
                        {
                            while(true)
                            {
                                nextNode = currentNode.Parent;
                                if(nextNode.Right != currentNode)
                                {
                                    currentNode = nextNode;
                                    continue;
                                }
                                else
                                {
                                    node = nextNode;
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            statusBits = IsBeforeLowestBit;
                            return false;
                        }
                    }
                    else
                    {
                        currentNode = nextNode;
                        while(true)
                        {
                            nextNode = currentNode.Right;
                            if(nextNode != localLoopbackNode)
                            {
                                currentNode = nextNode;
                                continue;
                            }
                            else
                            {
                                node = currentNode;
                                return true;
                            }
                        }
                    }
                }
            }
            else
            {
                return HandleStatus();
            }
        }

        public void Reset()
        {
            if(!isGoingDown)
            {
                statusBits = IsStartingEnumeration | IsBeforeLowestBit;
            }
            else
            {
                statusBits = IsStartingEnumeration | IsAfterHighestBit;
            }
        }

        #region explicit interface for IDictionaryEnumerator

        System.Collections.DictionaryEntry System.Collections.IDictionaryEnumerator.Entry
        {
            get
            {
                return new System.Collections.DictionaryEntry(Current.Key, Current.Value);
            }
        }

        object System.Collections.IDictionaryEnumerator.Key
        {
            get
            {
                return Current.Key;
            }
        }

        object System.Collections.IDictionaryEnumerator.Value
        {
            get
            {
                return Current.Value;
            }
        }


        #endregion

        #region explicit interface for IEnumerable

        object System.Collections.IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        #endregion

        #region explicit interface for IDisposable

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063", Justification = "The IDisposable interface is created only for compatability with IEnumerable.  We don't want to expose it as public")]
        void IDisposable.Dispose()
        {
        }

        #endregion

        #region private members

        private bool HandleStatus()
        {
            uint status = statusBits;
            TreeDictionary<TKey, TValue>.TreeNode currentNode;
            if(!isGoingDown)
            {
                if((status & IsBeforeLowestBit) != 0)
                {
                    currentNode = loopbackNode.Left;
                    if(currentNode == loopbackNode)
                    {
                        statusBits = IsAfterHighestBit;
                        return false;
                    }
                    else
                    {
                        node = currentNode;
                        statusBits = 0;
                        return true;
                    }
                }
                else
                {
                    if((status & IsAfterHighestBit) == 0)
                    {
                        Debug.Assert(node != loopbackNode);
                        Debug.Assert((status & IsStartingEnumeration) != 0);
                        statusBits = 0;
                        return true;
                    }
                    else
                    {
                        if((status & IsStartingEnumeration) != 0)
                        {
                            statusBits = IsAfterHighestBit;
                            return false;
                        }
                        else
                        {
                            throw new InvalidOperationException("TreeDictionaryKeyValuePairEnumerator<TKey, TValue>.MoveNext() cannot go past the end of the tree");
                        }
                    }
                }
            }
            else
            {
                if((status & IsAfterHighestBit) != 0)
                {
                    currentNode = loopbackNode.Right;
                    if(currentNode == loopbackNode)
                    {
                        statusBits = IsBeforeLowestBit;
                        return false;
                    }
                    else
                    {
                        node = currentNode;
                        statusBits = 0;
                        return true;
                    }
                }
                else
                {
                    if((status & IsBeforeLowestBit) == 0)
                    {
                        Debug.Assert(node != loopbackNode);
                        Debug.Assert((status & IsStartingEnumeration) != 0);
                        statusBits = 0;
                        return true;
                    }
                    else
                    {
                        if((status & IsStartingEnumeration) != 0)
                        {
                            statusBits = IsBeforeLowestBit;
                            return false;
                        }
                        else
                        {
                            throw new InvalidOperationException("TreeDictionaryKeyValuePairEnumerator<TKey, TValue>.MoveNext() cannot go past the beginning of the tree");
                        }
                    }
                }
            }
        }

        #endregion
    }
}
