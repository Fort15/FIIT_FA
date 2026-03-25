using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null) return (null, null);

        int cmp = Comparer.Compare(root.Key, key);

        if (cmp <= 0) // rootkey <= key
        {
            // all left <= key
            var (newLeft, newRight) = Split(root.Right, key);

            root.Right = newLeft;

            root.Right?.Parent = root;
            root.Parent = null;

            return (root, newRight);
        }
        else
        {
            var (newLeft, newRight) = Split(root.Left, key);
        
            root.Left = newRight;
            
            root.Left?.Parent = root;
            root.Parent = null;
            
            return (newLeft, root);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null) return right;
        if (right == null) return left;

        if (left.Priority > right.Priority)
        {
            // new root - left
            left.Right = Merge(left.Right, right);
            left.Right?.Parent = left;
            left.Parent = null;
            return left;
        }
        else
        {
            // new root - right
            right.Left = Merge(left, right.Left);
            right.Left?.Parent = right;
            right.Parent = null;
            return right;
        }
    }
    

    public override void Add(TKey key, TValue value)
    {
        var oldnode = FindNode(key);
        if (oldnode != null)
        {
            oldnode.Value = value;
            return;
        }
        var node = CreateNode(key, value);

        var (left, right) = Split(this.Root, key);

        var newRoot = Merge(Merge(left, node), right);

        this.Root = newRoot;
        Count++;
    }



    // analog split but not <= just <
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) AntiSplit(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null) return (null, null);

        int cmp = Comparer.Compare(root.Key, key);

        if (cmp < 0) // rootkey < key
        {
            var (newLeft, newRight) = AntiSplit(root.Right, key);

            root.Right = newLeft;

            root.Right?.Parent = root;
            root.Parent = null;

            return (root, newRight);
        }
        else
        {
            var (newLeft, newRight) = AntiSplit(root.Left, key);
        
            root.Left = newRight;
            
            root.Left?.Parent = root;
            root.Parent = null;
            
            return (newLeft, root);
        }
    }


    public override bool Remove(TKey key)
    {
        if (!ContainsKey(key)) return false;

        // left <= key right > key
        var (left, right) = Split(this.Root, key);

        // leftOfLeft < key removedNode >= key (in left only <= key)
        var (leftOfLeft, removedNode) = AntiSplit(left, key);

        this.Root = Merge(leftOfLeft, right);
        Count--;
        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey, TValue>(key, value);
    }
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode)
    {
    }
    
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child)
    {
    }
    
}