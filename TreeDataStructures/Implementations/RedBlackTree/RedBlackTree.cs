using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new RbNode<TKey, TValue>(key, value);
    }

    private static RbColor GetColor(RbNode<TKey, TValue>? node)
        => node?.Color ?? RbColor.Black;

    private static RbNode<TKey, TValue>? Minimum(RbNode<TKey, TValue>? node)
    {
        while (node?.Left != null)
        {
            node = node.Left;
        }

        return node;
    }

    private void FixInsert(RbNode<TKey, TValue> node)
    {
        var current = node;

        while (current.Parent != null && current.Parent.Color == RbColor.Red)
        {
            var parent = current.Parent;
            var grandparent = parent.Parent!;
            var uncle = parent.IsLeftChild ? grandparent.Right : grandparent.Left;

            // Uncle is Red
            if (GetColor(uncle) == RbColor.Red)
            {
                parent.Color = RbColor.Black;
                uncle?.Color = RbColor.Black;
                grandparent.Color = RbColor.Red;
                current = grandparent;
                continue;
            }

            // Uncle is Black
            if (parent.IsLeftChild)
            {
                if (current.IsRightChild)
                {
                    RotateLeft(parent);
                    current = parent;
                    parent = current.Parent!;
                }

                parent.Color = RbColor.Black;
                grandparent.Color = RbColor.Red;
                RotateRight(grandparent);
            }
            else
            {
                if (current.IsLeftChild)
                {
                    RotateRight(parent);
                    current = parent;
                    parent = current.Parent!;
                }

                parent.Color = RbColor.Black;
                grandparent.Color = RbColor.Red;
                RotateLeft(grandparent);
            }
        }

        Root?.Color = RbColor.Black;
    }

    private void FixDelete(RbNode<TKey, TValue>? node, RbNode<TKey, TValue>? parent)
    {
        var current = node;
        var currentParent = parent;

        while (current != Root && GetColor(current) == RbColor.Black)
        {
            if (currentParent == null)
            {
                break;
            }

            var currentIsLeftChild = current?.IsLeftChild ?? currentParent.Left == null;

            if (currentIsLeftChild)
            {
                var sibling = currentParent.Right;

                if (GetColor(sibling) == RbColor.Red)
                {
                    sibling!.Color = RbColor.Black;
                    currentParent.Color = RbColor.Red;
                    RotateLeft(currentParent);
                    sibling = currentParent.Right;
                }

                if (GetColor(sibling?.Left) == RbColor.Black &&
                    GetColor(sibling?.Right) == RbColor.Black)
                {
                    if (sibling != null)
                    {
                        sibling.Color = RbColor.Red;
                    }
                    current = currentParent;
                    currentParent = current.Parent;
                }
                else
                {
                    if (GetColor(sibling?.Right) == RbColor.Black)
                    {
                        if (sibling?.Left != null)
                        {
                            sibling.Left.Color = RbColor.Black;
                        }

                        sibling!.Color = RbColor.Red;
                        RotateRight(sibling);
                        sibling = currentParent.Right;
                    }

                    sibling!.Color = currentParent.Color;
                    currentParent.Color = RbColor.Black;
                    if (sibling.Right != null)
                    {
                        sibling.Right.Color = RbColor.Black;
                    }
                    RotateLeft(currentParent);
                    current = Root;
                    currentParent = null;
                }
            }
            else
            {
                var sibling = currentParent.Left;

                if (GetColor(sibling) == RbColor.Red)
                {
                    sibling!.Color = RbColor.Black;
                    currentParent.Color = RbColor.Red;
                    RotateRight(currentParent);
                    sibling = currentParent.Left;
                }

                if (GetColor(sibling?.Left) == RbColor.Black &&
                    GetColor(sibling?.Right) == RbColor.Black)
                {
                    if (sibling != null)
                    {
                        sibling.Color = RbColor.Red;
                    }
                    current = currentParent;
                    currentParent = current.Parent;
                }
                else
                {
                    if (GetColor(sibling?.Left) == RbColor.Black)
                    {
                        if (sibling?.Right != null)
                        {
                            sibling.Right.Color = RbColor.Black;
                        }

                        sibling!.Color = RbColor.Red;
                        RotateLeft(sibling);
                        sibling = currentParent.Left;
                    }

                    sibling!.Color = currentParent.Color;
                    currentParent.Color = RbColor.Black;
                    if (sibling.Left != null)
                    {
                        sibling.Left.Color = RbColor.Black;
                    }
                    RotateRight(currentParent);
                    current = Root;
                    currentParent = null;
                }
            }
        }

        if (current != null)
        {
            current.Color = RbColor.Black;
        }

        Root?.Color = RbColor.Black;
    }

    public override bool Remove(TKey key)
    {
        var node = FindNode(key);
        if (node == null)
        {
            return false;
        }

        var removedColor = node.Color;
        RbNode<TKey, TValue>? fixNode;
        RbNode<TKey, TValue>? fixParent;

        if (node.Left == null)
        {
            fixNode = node.Right;
            fixParent = node.Parent;
            Transplant(node, node.Right);
        }
        else if (node.Right == null)
        {
            fixNode = node.Left;
            fixParent = node.Parent;
            Transplant(node, node.Left);
        }
        else
        {
            var successor = Minimum(node.Right)!;
            removedColor = successor.Color;
            fixNode = successor.Right;

            if (successor.Parent == node)
            {
                fixParent = successor;
            }
            else
            {
                fixParent = successor.Parent;
                Transplant(successor, successor.Right);
                successor.Right = node.Right;
                successor.Right.Parent = successor;
            }

            Transplant(node, successor);
            successor.Left = node.Left;
            successor.Left.Parent = successor;
            successor.Color = node.Color;

            if (fixParent == node)
            {
                fixParent = successor;
            }
        }

        Count--;
        if (removedColor == RbColor.Black)
        {
            FixDelete(fixNode, fixParent);
        }

        return true;
    }

    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        FixInsert(newNode);
    }

    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
    }
}
