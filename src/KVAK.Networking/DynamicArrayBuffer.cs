using System.Diagnostics;

namespace KVAK.Networking;

/// <summary>
/// A queue-like data structure that stores elements of a generic type <typeparamref name="TElement"/>.
/// It is optimized such that an entire array of elements (chunks) can be efficiently added to the end.
/// </summary>
/// <typeparam name="TElement">Type of single element</typeparam>
public class DynamicArrayBuffer<TElement> : IDisposable
{
    private bool _disposed = false;
    
    // Internally we store references to the chunks in the order they were added in a LinkedList
    private readonly LinkedList<ArraySegment<TElement>> _data = new();
    
    /// <summary>
    /// Number of elements stored within the dynamic buffer.
    /// </summary>
    public int Length { get; private set; } = 0;

    /// <summary>
    /// Adds elements contained within the chunk of data to the end of the dynamic buffer.
    /// </summary>
    /// <param name="chunk">An array containing the elements to be added</param>
    public void Add(TElement[] chunk)
    {
        ArraySegment<TElement> chunkSegment = new ArraySegment<TElement>(chunk);
        Length += chunk.Length;
        _data.AddLast(chunkSegment);
    }

    /// <summary>
    /// Adds elements contained within the array segment to the end of the dynamic buffer.
    /// </summary>
    /// <param name="chunkSegment">A segment of an array containing the elements to be added</param>
    public void Add(ArraySegment<TElement> chunkSegment)
    {
        Length += chunkSegment.Count;
        _data.AddLast(chunkSegment);
    }
    
    /// <summary>
    /// <b>Copies</b> the first <paramref name="len"/> elements from the dynamic buffer and returns them.
    /// </summary>
    /// <param name="len">Number of elements to go over</param>
    /// <returns>An array containing the elements</returns>
    /// <exception cref="ArgumentException">When <paramref name="len"/> is not a positive integer or requesting more elements than available</exception>
    public TElement[] PeakFirst(int len)
    {
        return _QueryFirst(len, false);
    }
    
    /// <summary>
    /// <b>Deletes</b> the first <paramref name="len"/> elements from the dynamic buffer and returns them.
    /// </summary>
    /// <param name="len">Number of elements to go over</param>
    /// <returns>An array containing the elements</returns>
    /// <exception cref="ArgumentException">When <paramref name="len"/> is not a positive integer or requesting more elements than available</exception>
    public TElement[] RemoveFirst(int len)
    {
        return _QueryFirst(len, true);
    }
    
    // Returns a copy of the first 'len' elements and optionally
    // deletes them from the dynamic buffer along the way if delete parameter is true
    // Throws an ArgumentException if the requested length is either <= 0 or it is greater than the number of elements available
    private TElement[] _QueryFirst(int len, bool delete)
    {
        if (len <= 0) throw new ArgumentException("Non-positive length is invalid input!");
        if (len > Length) throw new ArgumentException("Requesting more adata to remove than available!");
        
        // This will hold the N elements from the structure
        TElement[] outBuffer = new TElement[len];
        
        int remaining = len;
        LinkedListNode<ArraySegment<TElement>>? chunkSegmentNode = _data.First;
        while (remaining != 0)
        {
            // Sanity check
            Debug.Assert(chunkSegmentNode != null, "Chunk segment node shouldn't ever be null because len > Length");
            
            // The index of the latest added element within the output buffer
            int outBufferOffset = len - remaining;

            ArraySegment<TElement> chunkSegment = chunkSegmentNode!.ValueRef;
            
            if ((remaining - chunkSegment.Count) >= 0) // We need the whole retrieved chunk segment copied to the output buffer
            {
                // The corresponding offseted array segment within the buffer to copy the data to from the chunk data segment
                var outBufferSegment = new ArraySegment<TElement>(outBuffer, outBufferOffset,chunkSegment.Count);
                chunkSegment.CopyTo(outBufferSegment);
                
                remaining -= chunkSegment.Count;
                LinkedListNode<ArraySegment<TElement>>? nextChunkSegmentNode = chunkSegmentNode.Next;
                if (delete) {
                    _data.Remove(chunkSegmentNode); // chunkSegment is the head of the list so this operation is still O(1)
                }
                chunkSegmentNode = nextChunkSegmentNode;
            }
            else // We need only part of the retrieved chunk segment copied into the output buffer, the rest of the chunk is re-added to the structure
            {
                // Segment into the remaining elements within the chunk needed to fill the buffer 
                var remainingChunk = chunkSegment.Slice(0, remaining);
                // Segment into the buffer to be filled by remaining elements within the chunk
                var outBufferSegment = new ArraySegment<TElement>(outBuffer, outBufferOffset,remaining);
                remainingChunk.CopyTo(outBufferSegment);
                
                // Segment of chunk that is kept in the data structure
                var keptSegment = chunkSegment.Slice(remaining);
                if (delete)
                {
                    chunkSegmentNode.Value = keptSegment;
                }
                
                remaining = 0;
            }
        }

        if (delete)
        {
            Length -= len;
        } 
        
        return outBuffer;
    }
    
    public void Dispose()
    {
        // Dispose of managed & unmanaged resources.
        Dispose(true);
        // Suppress finalization.
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            foreach (var segment in _data)
            {
                foreach (var element in segment)
                {
                    if (element is IDisposable disposableElement)
                    {
                        disposableElement.Dispose();           
                    }
                }
            }
            
            _data.Clear();
        }
        
        _disposed = true;
    }
}