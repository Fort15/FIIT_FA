using TreeDataStructures.Implementations.BST;  // Это для BinarySearchTree
using TreeDataStructures.Core;                  // Это для базовых классов (может не понадобиться)

var tree = new BinarySearchTree<int, string>();

// Добавляем элементы
tree.Add(5, "пять");
tree.Add(3, "три");
tree.Add(7, "семь");
tree.Add(2, "два");
tree.Add(4, "четыре");

// Проверяем размер
Console.WriteLine($"Размер дерева: {tree.Count}");