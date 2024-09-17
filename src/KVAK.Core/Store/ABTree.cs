namespace KVAK.Core.Store;

/// <summary>
/// Holds the key and its associated DataUnit
/// </summary>
public class KeyData
{
    public readonly string Key;
    public StoreDataUnit Data;

    public KeyData(string key, StoreDataUnit data)
    {
        Key = key;
        Data = data;
    }
}

/// <summary>
/// A node in (a,b)-tree
/// </summary>
/// <see cref="AbTree"/>
public class AbTreeNode
{
    /// <summary>
    /// A list of <see cref="KeyData"/> that hold keys and their dataUnit
    /// </summary>
    public List<KeyData> KeysWithData;
    /// <remarks>
    /// There is 1 more child than there are keys, except for internal nodes on the last layer, which do not have any children.
    /// </remarks>
    public List<AbTreeNode> Children;

    public AbTreeNode(List<KeyData> keysWithData, List<AbTreeNode> children)
    {
        KeysWithData = keysWithData;
        Children = children;
    }

    /// <summary>
    /// Deletes the KeyData entry given the key. If key not found, doesn't do anything.
    /// </summary>
    /// <param name="key">Associated key</param>
    public void DeleteKeyData(string key)
    {
        for (int i = 0; i < KeysWithData.Count; i++)
        {
            if (KeysWithData[i].Key == key)
            {
                KeysWithData.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Gets the index into the <see cref="KeysWithData"/> array, given the key
    /// </summary>
    /// <param name="key">One of the keys of the node</param>
    /// <returns>An index if found, otherwise -1</returns>
    public int GetKeyDataIndex(string key)
    {
        for (int i = 0; i < KeysWithData.Count; i++)
        {
            if (KeysWithData[i].Key == key)
            {
                return i;
            }
        }
        
        return -1;
    }

    /// <summary>
    /// Checks if this node contains the passed key
    /// </summary>
    /// <param name="key">Key to check for existence</param>
    /// <returns>True if it exists</returns>
    public bool HasKeyData(string key)
    {
        return KeysWithData.Any(x => x.Key == key);
    }
    
    /// <summary>
    /// Retrieves the KeyData from the current node, given the key
    /// </summary>
    /// <param name="key">One of the keys of the current node</param>
    /// <returns><see cref="KeyData"/> for the key, or null</returns>
    /// <exception cref="ArgumentNullException">When the key does not exist in the node</exception>
    public KeyData? GetKeyData(string key)
    {
        return KeysWithData.Find((x => x.Key == key));
    }

    /// <summary>
    /// Inserts the KeyData into the node.
    /// </summary>
    /// <param name="data">KeyData to insert</param>
    /// <remarks>The caller needs to ensure that the node does not exceed the max. number of keys!</remarks>
    public void InsertKeyData(KeyData data)
    {
        int i = 0;
        while ((i < KeysWithData.Count) && !Utils.LexicographicComparator(data.Key, KeysWithData[i].Key))
        {
            i++;
        }
        
        KeysWithData.Insert(i, data);
    }
    
    /// <summary>
    /// Calculates the index of the child, given a key
    /// </summary>
    /// <param name="key">A key that can be located in the children subtree of this node</param>
    /// <returns>An index in the <see cref="Children"/> array</returns>
    /// <exception cref="ArgumentNullException">When the key exists in this node or the node does not have any children</exception>
    public int GetChildIndexFromKey(string key)
    {
        if (Children.Count == 0)
        {
            throw new ArgumentException("This node is a leaf node");
        }
        
        int i = 0;
        while ((i < KeysWithData.Count) && !Utils.LexicographicComparator(key, KeysWithData[i].Key))
        {
            if (KeysWithData[i].Key == key)
            {
                throw new ArgumentException("The provided key does not belong to any of the nodes in the subtree of this node");
            }
            
            i++;
        }
        
        i = Math.Max(0, i);
        i = Math.Min(Children.Count - 1, i);
        
        return i;
    }
}

/// <inheritdoc />
/// <remarks>Nodes on the last internal layer of the tree do not have children array filled with nulls, but it is rather an empty array.
/// External nodes are thus purely virtual and do not have any representation in the implementation whatsoever.</remarks>
public class AbTree : IBinaryStore
{
    private readonly int _a;
    private readonly int _b;
    private AbTreeNode? _root;
    
    /// <summary>
    /// Constructs an (a,b)-tree.
    /// </summary>
    /// <param name="a">Specifies parameter a</param>
    /// <param name="b">Specifies parameter b</param>
    /// <exception cref="ArgumentException">When the a,b parameters are not valid</exception>
    public AbTree(int a, int b)
    {
        if (a < 2 || b < (2 * a - 1))
        {
            throw new ArgumentException("The parameters are not valid for an (a,b)-tree!");
        }

        _a = a;
        _b = b;
    }
    public void Remove(string key)
    {
        // We try to find a node that contains the key to remove while we keep track of the visited nodes in a stack
        Stack<AbTreeNode> stack = new Stack<AbTreeNode>();
        AbTreeNode? nextNode = _root;
        while (nextNode is AbTreeNode node && !node.HasKeyData(key))
        {
            stack.Push(node);
            
            // Check if the next node is an external node
            if (node.Children.Count == 0)
            {
                nextNode = null;
                continue;
            }
            
            nextNode = node.Children[node.GetChildIndexFromKey(key)];
        }
        
        // The key is not in any node of the tree. No need to do anything.
        if (nextNode == null)
        {
            return; 
        }
        
        stack.Push(nextNode); // Apend to the stack the final node containing the key

        var originalKeyNode = stack.Peek();
        string adjustedKeyToDelete = key;
        
        // Transform the problem of deleting a key that is not in any of the nodes in the last internal layer to deleting a key in one of those nodes.
        // TLDR: Similar to deletion in binary trees - finding a successor to the key we want to delete.
        if (originalKeyNode.Children.Count > 0)
        {
            int index = originalKeyNode.GetKeyDataIndex(key);
            var leftNode = originalKeyNode.Children[index];
            
            // Find the successor
            AbTreeNode? rightMostNode = leftNode;
            while (rightMostNode is AbTreeNode node)
            {
                stack.Push(node);
                
                if (node.Children.Count == 0)
                {
                    rightMostNode = null;
                    continue;
                }

                rightMostNode = node.Children.Last();
            }


            var successorKeyData = stack.Peek().KeysWithData.Last();
            originalKeyNode.KeysWithData[index] = successorKeyData;
            
            adjustedKeyToDelete = successorKeyData.Key;
        }
        
        // We are now deleting a key in the last internal layer of the ab tree
        var deleteNode = stack.Pop();
        deleteNode.DeleteKeyData(adjustedKeyToDelete);
        
        // Resolve underflowed nodes up until the root node
        while (deleteNode != _root && deleteNode.KeysWithData.Count < _a - 1)
        {
            var parentNode = stack.Pop();
            
            var childIndexInParent = parentNode.Children.IndexOf(deleteNode);
            var siblingIndexInParent = childIndexInParent >= 1 ? childIndexInParent - 1 : childIndexInParent + 1; // Prefer the left sibling
            var siblingNode = parentNode.Children[siblingIndexInParent];
            
            var pivotKeyDataIndexInParent = Math.Min(childIndexInParent, siblingIndexInParent);
            var pivotKeyDataInParent = parentNode.KeysWithData[pivotKeyDataIndexInParent];
            
            // Name nodes based on which side of the pivot key they are on 
            AbTreeNode leftNode;
            AbTreeNode rightNode;
            if (childIndexInParent > siblingIndexInParent)
            {
                leftNode = siblingNode;
                rightNode = deleteNode;
            }
            else
            {
                leftNode = deleteNode;
                rightNode = siblingNode;
            }
            
            
            // If the sibling has the min. number of keys, we can merge
            if (siblingNode.KeysWithData.Count == _a - 1)
            {
                AbTreeNode mergedNode = new AbTreeNode(leftNode.KeysWithData.Concat([pivotKeyDataInParent]).Concat(rightNode.KeysWithData).ToList(), leftNode.Children.Concat(rightNode.Children).ToList());
                
                parentNode.KeysWithData.RemoveAt(pivotKeyDataIndexInParent);
                // Delete old nodes and add the newly merged node in their place
                parentNode.Children.Insert(siblingIndexInParent, mergedNode);
                parentNode.Children.Remove(leftNode);
                parentNode.Children.Remove(rightNode);
            }
            else
            {
                // Otherwise we kind of "rotate" keys without changing the number of keys in the parent
                if (siblingNode == rightNode)
                {
                    deleteNode.KeysWithData.Add(pivotKeyDataInParent);
                    parentNode.KeysWithData[pivotKeyDataIndexInParent] = siblingNode.KeysWithData.First();
                    siblingNode.KeysWithData.RemoveAt(0);

                    if (siblingNode.Children.Count > 0)
                    {
                        deleteNode.Children.Add(siblingNode.Children.First());
                        siblingNode.Children.RemoveAt(0);   
                    }
                }
                else
                {
                    deleteNode.KeysWithData.Insert(0, pivotKeyDataInParent);
                    parentNode.KeysWithData[pivotKeyDataIndexInParent] = siblingNode.KeysWithData.Last();
                    siblingNode.KeysWithData.RemoveAt(siblingNode.KeysWithData.Count - 1);

                    if (siblingNode.Children.Count > 0)
                    {
                        deleteNode.Children.Insert(0, siblingNode.Children.Last());
                        siblingNode.Children.RemoveAt(siblingNode.Children.Count - 1);   
                    }
                }
            }
            
            deleteNode = parentNode;
        }
        
        // The root node is in the deleteNode variable after the while loop, so we check if it's empty because of a previous merge on a lower level
        if (deleteNode.KeysWithData.Count == 0)
        {
            _root = deleteNode.Children.Count > 0 ? deleteNode.Children.First() : null;
        }
    }
    
    public void Add(string key, StoreDataUnit dataUnit)
    {
        if (_root == null)
        {
            _root = new AbTreeNode([new KeyData(key, dataUnit)], []);
            return;
        }
        
        // First we find a node to insert the key to, but we keep track of the visited nodes in a stack
        Stack<AbTreeNode> stack = new Stack<AbTreeNode>();
        AbTreeNode? nextNode = _root;
        KeyData? keyData = nextNode?.GetKeyData(key);
        // We traverse downwards until we find a pre-existing node with the key or a corresponding node in the last internal layer
        while (nextNode is AbTreeNode node && keyData == null)
        {
            stack.Push(node);
            
            // Check if the next node is an external node
            if (node.Children.Count == 0)
            {
                nextNode = null;
                continue;
            }
            
            nextNode = node.Children[node.GetChildIndexFromKey(key)];
            keyData = nextNode?.GetKeyData(key);
        }

        if (keyData != null)
        {
            // The key was already in the node - just overwrite it
            keyData.Data = dataUnit;
        }
        else
        {
            // We need to add the key to the node first
            var insertNode = stack.Pop();
            insertNode.InsertKeyData(new KeyData(key, dataUnit));
            
            // Resolve overflowed nodes up the path
            while (insertNode.KeysWithData.Count == _b)
            {
                // Halve the node we inserted the new key to. Do not include the middle key in the newly created nodes.
                // Note: Prefers the left-hand key in case the number of keys is even.
                int keyMiddleIndex = (insertNode.KeysWithData.Count - 1) / 2;
                KeyData middleKeyData = insertNode.KeysWithData[keyMiddleIndex];
                bool isInternalNode = insertNode.Children.Count > 0;
                
                var leftNodeKeyData = insertNode.KeysWithData.GetRange(0, keyMiddleIndex);
                var leftNodeChildren = isInternalNode ? insertNode.Children.GetRange(0, keyMiddleIndex + 1) : new List<AbTreeNode>();

                var rightNodeKeyData = insertNode.KeysWithData.GetRange(keyMiddleIndex + 1,
                    insertNode.KeysWithData.Count - keyMiddleIndex - 1);
                var rightNodeChildren = isInternalNode ? insertNode.Children.GetRange(keyMiddleIndex + 1, insertNode.Children.Count - (keyMiddleIndex + 1)) : new List<AbTreeNode>();
                
                var leftNode = new AbTreeNode(leftNodeKeyData, leftNodeChildren);
                var rightNode = new AbTreeNode(rightNodeKeyData, rightNodeChildren);
                
                
                AbTreeNode parentNode;
                if (stack.Count > 0)
                {
                    parentNode = stack.Pop();
                    // Lift the middle key upwards to the parentNode
                    int childIndexInParent = parentNode.GetChildIndexFromKey(key);
                    parentNode.KeysWithData.Insert(childIndexInParent, middleKeyData);
                
                    // Connect the new nodes to the parent, to the left and right of the middle key
                    parentNode.Children.RemoveAt(childIndexInParent);
                    parentNode.Children.Insert(childIndexInParent, rightNode);
                    parentNode.Children.Insert(childIndexInParent, leftNode);
                }
                else
                {
                    // The root node has overflowed, we need to create a new one
                    parentNode = new AbTreeNode([middleKeyData], [leftNode, rightNode]);
                    _root = parentNode;
                }
                
                insertNode = parentNode;
            }
            
        }
        
    }
    
    public StoreDataUnit? Find(string key)
    {
        AbTreeNode? nextNode = _root;
        
        // We traverse downwards until we find the node with the key, or we hit an external node (null)
        while (nextNode is AbTreeNode node && node.GetKeyData(key) == null)
        {
            // Check if the next node is an external node
            if (node.Children.Count == 0)
            {
                nextNode = null;
                continue;
            }
            
            nextNode = node.Children[node.GetChildIndexFromKey(key)];
        }
        
        if (nextNode is AbTreeNode searchedNode)
        {
            return ((KeyData) searchedNode.GetKeyData(key)!).Data;
        }
        
        // nextNode was null, therefore the searched key was not in the tree
        return null;
    }
}