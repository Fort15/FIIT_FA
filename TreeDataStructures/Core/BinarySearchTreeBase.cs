using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    private class MyTreeEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly IEnumerator<TreeEntry<TKey, TValue>> _inner;

        public MyTreeEnumerator(IEnumerator<TreeEntry<TKey, TValue>> inner)
        {
            _inner = inner;
        }

        public KeyValuePair<TKey, TValue> Current 
            => new KeyValuePair<TKey, TValue>(_inner.Current.Key, _inner.Current.Value);

        object IEnumerator.Current => Current;

        public bool MoveNext() => _inner.MoveNext();
        public void Reset() => _inner.Reset();
        public void Dispose() => _inner.Dispose();
    }

    public ICollection<TKey> Keys
    {
        get
        {
            var keys = new List<TKey>();
            foreach (var node in InOrder())
            {
                keys.Add(node.Key);
            }
            return keys;
        }
    }
    public ICollection<TValue> Values
    {
        get
        {
            var values = new List<TValue>();
            foreach (var node in InOrder())
            {
                values.Add(node.Value);
            }
            return values;
        }
    }
    
    
    public virtual void Add(TKey key, TValue value)
    {
        TNode newNode = CreateNode(key, value);

        if (Root == null)
        {
            Root = newNode;
            this.Count++;
            OnNodeAdded(newNode);
            return;
        }

        TNode? current = Root;
        TNode? parent = null;
        int cmp = 0;
        while (current != null)
        {
            parent = current;
            cmp = Comparer.Compare(key, current.Key);

            if (cmp == 0)
            {
                current.Value = value;
                return;
            }

            current = (cmp < 0) ? current.Left : current.Right;
        }
        
        if (cmp < 0)
        {
            parent?.Left = newNode;
        }
        else
        {
            parent?.Right = newNode;
        }

        newNode.Parent = parent;
        this.Count++;
        OnNodeAdded(newNode);
    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    
    protected virtual void RemoveNode(TNode node)
    {
        if (node.Left == null && node.Right == null)
        {
            if (node.Parent == null)
            {
                Root = null;
            }
            else if (node.IsLeftChild)
            {
                node.Parent.Left = null;
            }
            else
            {
                node.Parent.Right = null;
            }

            OnNodeRemoved(node.Parent, null);
            return;
        }

        else if (node.Left == null)
        {
            Transplant(node, node.Right);
            OnNodeRemoved(node.Parent, node.Right);
            return;
        }
        else if (node.Right == null)
        {
            Transplant(node, node.Left);
            OnNodeRemoved(node.Parent, node.Left);
            return;
        }
        else
        {
            // самый левый из ПП
            TNode minRight = node.Right;
            while (minRight.Left != null)
            {
                minRight = minRight.Left;
            }

            TKey minRightkey = minRight.Key;
            TValue minRightvalue = minRight.Value;

            if (minRight.Left == null && minRight.Right == null)
            {
                if (minRight.IsLeftChild)
                {
                    minRight?.Parent?.Left = null;
                }
                else
                {
                    minRight?.Parent?.Right = null;
                }
            }
            else if (minRight.Right != null)
            {
                Transplant(minRight, minRight.Right);
            }


            node.Key = minRightkey;
            node.Value = minRightvalue;

            OnNodeRemoved(node.Parent, node);
        }
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        if (x?.Right == null) throw new InvalidOperationException("Нельзя сделать левый поворот, нет правого потомка");

        TNode y = x.Right;

        // ЛП y -> ПП x
        x.Right = y.Left;
        y.Left?.Parent = x;

        // родитель x -> родитель y
        y.Parent = x.Parent;
        if (x.Parent == null)
        {
            this.Root = y;
        }
        else if (x.IsLeftChild)
        {
            x.Parent.Left = y;
        }
        else
        {
            x.Parent.Right = y;
        }

        // x -> левый ребенок y
        y.Left = x;
        x.Parent = y;
        
    }

    protected void RotateRight(TNode y)
    {
        if (y?.Left == null) throw new InvalidOperationException("Нельзя сделать правый поворот, нет левого потомка");

        TNode x = y.Left;

        // ПП x -> ЛП y
        y.Left = x.Right;
        x.Right?.Parent = y;

        // parent y -> parent x
        x.Parent = y.Parent;
        if (y.Parent == null)
        {
            Root = x;
        }
        else if (y.IsLeftChild)
        {
            y.Parent.Left = x;
        }
        else
        {
            y.Parent.Right = x;
        }

        // y -> right child x
        x.Right = y;
        y.Parent = x;
    }
    
    protected void RotateBigLeft(TNode x)
    {
        if (x?.Right == null) throw new InvalidOperationException("Нельзя сделать большой левый поворот, нет правого потомка");

        RotateRight(x.Right);
        RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
        if (y?.Left == null) throw new InvalidOperationException("Нельзя сделать большой правый поворот, нет левого потомка");

        RotateLeft(y.Left);
        RotateRight(y);
    }
    
    protected void RotateDoubleLeft(TNode grandparent)
    {
        if (grandparent?.Right == null) throw new InvalidOperationException("Нельзя сделать двойной левый поворот");

        TNode parent = grandparent.Right;

        RotateLeft(grandparent);
        RotateLeft(parent);
    }
    
    protected void RotateDoubleRight(TNode grandparent)
    {
        if (grandparent?.Left == null) throw new InvalidOperationException("Нельзя сделать двойной правый поворот");

        TNode parent = grandparent.Left;

        RotateRight(grandparent);
        RotateRight(parent);
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() 
        => new TreeIterator(Root, TraversalStrategy.InOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() 
        => new TreeIterator(Root, TraversalStrategy.PreOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() 
        => new TreeIterator(Root, TraversalStrategy.PostOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.InOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    
    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TNode? _root;
        private TNode? _currentNode;
        private Stack<TNode> _mainStack;
        private Stack<TNode>? _postOrderStack;
        private readonly TraversalStrategy _strategy;
        private bool _IsFirstMove;
        
        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;

        
        public TreeEntry<TKey, TValue> Current
        {
            get
            {
                if (_currentNode != null)
                {
                    return new TreeEntry<TKey, TValue>(
                        _currentNode.Key, _currentNode.Value, GetHeight(_currentNode));
                } 
                throw new InvalidOperationException("_curentNode = null");
            }
        }
        object IEnumerator.Current => Current;

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _mainStack = new Stack<TNode>();
            _currentNode = null;
            _IsFirstMove = true;
            _postOrderStack = null;
        }

        // InOrder all left sons
        private void PushLeftBranch(TNode? node)
        {
            while (node != null)
            {
                _mainStack.Push(node);
                node = node.Left;
            }
        }

        // all right sons
        private void PushRightBranch(TNode? node)
        {
            while (node != null)
            {
                _mainStack.Push(node);
                node = node.Right;
            }
        }

        private void PreparePostOrderStack()
        {
            if (_root == null) return;

            _postOrderStack = new Stack<TNode>();

            Stack<TNode> tempStack = new Stack<TNode>();
            tempStack.Push(_root);

            while (tempStack.Count > 0)
            {
                TNode current = tempStack.Pop();

                _postOrderStack.Push(current);

                if (current.Left != null)
                {
                    tempStack.Push(current.Left);
                }
                if (current.Right != null)
                {
                    tempStack.Push(current.Right);        
                }
            }
        }

        private void PreparePostOrderReverseStack()
        {
            if (_root == null) return;

            _postOrderStack = new Stack<TNode>();

            Stack<TNode> tempStack = new Stack<TNode>();
            tempStack.Push(_root);

            while (tempStack.Count > 0)
            {
                TNode current = tempStack.Pop();

                _postOrderStack.Push(current);

                if (current.Right != null)
                {
                    tempStack.Push(current.Right);        
                }
                if (current.Left != null)
                {
                    tempStack.Push(current.Left);
                }
            }
        }

        private int GetHeight(TNode? node)
        {
            if (node == null) return -1;
            
            int leftheight = GetHeight(node.Left);
            int rightheight = GetHeight(node.Right);

            return 1 + Math.Max(leftheight, rightheight);
        }
        
        
        public bool MoveNext()
        {
            if (_strategy == TraversalStrategy.PostOrder)
            {
                if (_postOrderStack == null)
                {
                    PreparePostOrderStack();
                }

                if (_postOrderStack == null || _postOrderStack.Count == 0)
                {
                    return false;
                }

                _currentNode = _postOrderStack.Pop();
                return true;
            }
            if (_strategy == TraversalStrategy.PreOrderReverse)
            {
                if (_postOrderStack == null)
                {
                    PreparePostOrderReverseStack();
                }
                
                if (_postOrderStack == null || _postOrderStack.Count == 0)
                    return false;
                
                _currentNode = _postOrderStack?.Pop();
                return true;
            }
            if (_IsFirstMove)
            {
                _IsFirstMove = false;
                switch (_strategy)
                {
                    case TraversalStrategy.InOrder:
                        PushLeftBranch(_root);
                        break;
                    case TraversalStrategy.InOrderReverse:
                        PushRightBranch(_root);
                        break;
                    
                    case TraversalStrategy.PreOrder:
                    case TraversalStrategy.PostOrderReverse:
                        if (_root != null) _mainStack.Push(_root);
                        break;
                }
            }

            if (_mainStack.Count == 0) return false;

            _currentNode = _mainStack.Pop();

            switch (_strategy)
            {
                case TraversalStrategy.InOrder:
                    if (_currentNode.Right != null)
                        PushLeftBranch(_currentNode.Right);
                    break;

                case TraversalStrategy.InOrderReverse:
                    if (_currentNode.Left != null) 
                        PushRightBranch(_currentNode.Left);
                    break;

                case TraversalStrategy.PreOrder:
                    if (_currentNode.Right != null)
                        _mainStack.Push(_currentNode.Right);
                    if (_currentNode.Left != null)
                        _mainStack.Push(_currentNode.Left);
                    break;

                case TraversalStrategy.PostOrderReverse:
                    if (_currentNode.Left != null)
                        _mainStack.Push(_currentNode.Left);
                    if (_currentNode.Right != null)
                        _mainStack.Push(_currentNode.Right);
                    break;
            }

            return true;
        }
        
        public void Reset()
        {
            _mainStack.Clear();
            _currentNode = null;
            _IsFirstMove = true;

            if (_postOrderStack != null)
            {
                _postOrderStack.Clear();
            }
        }

        
        public void Dispose()
        {
            _mainStack.Clear();
            _currentNode = null;
            if (_postOrderStack != null)
            {
                _postOrderStack.Clear();
            }
        }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return new MyTreeEnumerator(InOrder().GetEnumerator());
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array == null)
        {
            throw new ArgumentNullException("array is null"); 
        }

        if (arrayIndex < 0)
        {
            throw new ArgumentOutOfRangeException("wrong index");
        }
        if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentException("doesn't have space");
        }

        int i = arrayIndex;
        foreach (var entry in InOrder())
        {
            array[i++] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}