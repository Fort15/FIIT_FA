using System.ComponentModel.DataAnnotations;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    

    private static int GetHeight(AvlNode<TKey, TValue>? node)
    {
        return node?.Height ?? 0;
    }

    private static void UpdateHeight(AvlNode<TKey, TValue> node)
    {
        if (node == null) return;
        node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
    }

    private static int GetBalance(AvlNode<TKey, TValue>? node)
    {
        if (node == null) return 0;
        return GetHeight(node.Right) - GetHeight(node.Left);
    }


    private AvlNode<TKey, TValue> RotateLeftAVL(AvlNode<TKey, TValue> x)
    {
        if (x.Right == null) return x;

        var y = x.Right;

        RotateLeft(x);

        UpdateHeight(x);
        UpdateHeight(y);
        return y;
    }


    private AvlNode<TKey, TValue> RotateRightAVL(AvlNode<TKey, TValue> x)
    {
        if (x.Left == null) return x;
        var y = x.Left;

        RotateRight(x);

        UpdateHeight(x);
        UpdateHeight(y);
        return y;
    }


    // private void ReplaceNodeInParent(AvlNode<TKey, TValue> oldNode, AvlNode<TKey, TValue>? newNode)
    // {
    //     var parent = oldNode.Parent;

    //     if (parent == null) Root = newNode;
    //     else if (parent.Left == oldNode) parent.Left = newNode;
    //     else parent.Right = newNode;
        
    //     if (newNode != null) newNode.Parent = parent;
    // }


    private AvlNode<TKey, TValue>? Balance(AvlNode<TKey, TValue>? node)
    {
        if (node == null) return null;

        UpdateHeight(node);
        int balance = GetBalance(node); 

        if (balance < -1)
        {
            // перекос влево
            if (node.Left == null) return node;
            
            int leftBalance = GetBalance(node.Left);
            if (leftBalance > 0)
            {
                node.Left = RotateLeftAVL(node.Left);
                return RotateRightAVL(node);
            }
            return RotateRightAVL(node);
        }
        if (balance > 1)
        {
            // перекос вправо
            if (node.Right == null) return node;

            int rightBalance = GetBalance(node.Right);

            if (rightBalance < 0)
            {
                node.Right = RotateRightAVL(node.Right);
                return RotateLeftAVL(node);
            }
            return RotateLeftAVL(node);
        }

        return node;
    }

    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        var current = newNode.Parent;
        while (current != null)
        {
            var balanced = Balance(current);

            current = balanced?.Parent;
        }
    }

    
    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        var current = parent ?? child;

        while (current != null)
        {
            var balanced = Balance(current);

            current = balanced?.Parent;
        }
    }
}