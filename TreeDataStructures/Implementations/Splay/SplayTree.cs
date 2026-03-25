using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }
    
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child) {}
    
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var node = FindNode(key);
        if (node == null)
        {
            value = default;
            return false;
        }
        value = node.Value;
        Splay(node);
        return true;
    }

    // поднять вверх, слить лп и пп
    public override bool Remove(TKey key)
    {
        var node = FindNode(key);
        if (node == null) return false;

        Splay(node);

        var left = node.Left;
        var right = node.Right;

        if (left == null)
        {
            this.Root = right;
            if (this.Root != null) this.Root.Parent = null;
        }
        else if (right == null)
        {
            this.Root = left;
            if (this.Root != null) this.Root.Parent = null;
        }
        else
        {   
            // max in left and splay it, merge with right
            left.Parent = null;

            var max = left;
            while (max.Right != null) max = max.Right;

            Splay(max);

            max.Right = right;
            right.Parent = max;

            this.Root = max;
        }

        this.Count--;
        return true;
    }

    public override bool ContainsKey(TKey key)
    {
        var node = FindNode(key);
        if (node == null) return false;
        
        Splay(node);
        return true;
    }

    private void Splay(BstNode<TKey, TValue> node)
    {
        if (node == null) return;

        while (node.Parent != null)
        {
            var parent = node.Parent;
            var grandparent = parent.Parent;

            if (grandparent == null)
            {
                // zig
                if (node.IsLeftChild) RotateRight(parent);
                else RotateLeft(parent);
            }
            else
            {
                if (node.IsLeftChild && parent.IsLeftChild)
                {
                    // zig zig
                    RotateDoubleRight(grandparent);
                }
                else if (node.IsRightChild && parent.IsRightChild)
                {
                    // zig zig
                    RotateDoubleLeft(grandparent);
                }
                else if (node.IsLeftChild && parent.IsRightChild)
                {
                    // zig zag
                    RotateBigLeft(grandparent);
                }
                else if (node.IsRightChild && parent.IsLeftChild)
                {
                    RotateBigRight(grandparent);
                }
            }
        }
    }
    
}
