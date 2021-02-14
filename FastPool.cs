using System;
using System.Collections.Generic;
using System.Numerics;

namespace Pool
{
    /// <summary>
    /// A fast unsafe pool of structs.
    /// </summary>
    public class FastPool<TObject> where TObject : struct
    {
        // We're doing 2^n sized arrays (16, 16, 32, 64, 128, ...) allocated as needed.
        private TObject[][] _pools = new TObject[12][];

        // Bitmap stores info if the given index is in use or not.
        private Bitmap _bitmap;

        // Highest index in the pools to be given out next.
        // Only grows.
        private uint _count;

        // Number of _pools allocated (technically the n in 2^n of allocated space).
        private ushort _size;

        // Holes to be given out after a _fake_ GC sweep.
        private Stack<uint> _holes = new Stack<uint>(16);

        /// <summary>
        /// Static default pool of <typeparamref name="TObject"/>.
        /// </summary>
        public static FastPool<TObject> Default = new FastPool<TObject>();

        public FastPool() : this(0) {}

        public FastPool(uint capacity)
        {
            // minimum capacity is 16 (idx 0..15) because there's no point
            // in allocating smaller arrays
            capacity = capacity < 16 ? 16 : capacity;
            _pools[0] = new TObject[16];
            _size = 3;
            var cap = BitOperations.Log2(capacity - 1);
            for(var i = 4; i <= cap; i++)
            {
                _pools[i-3] = new TObject[1 << i];
                _size = (ushort)i;
            }

            _bitmap = new Bitmap(1 << (_size + 1));
        }

        /// <summary>
        /// Allocates a new instance of <typeparamref name="TObject"/> in the pool.
        /// </summary>
        /// <param name="init">If <c>true</c> the struct is initialized with the default constructor (filled with 0).</param>
        public uint Create(bool init = false)
        {
            if (_holes.TryPop(out uint hole))
            {
                _bitmap.Set(hole, true);

                if (init)
                {
                    ref TObject instance = ref Get(hole);
                    instance = new TObject();
                }

                return hole;
            }

            if (BitOperations.Log2(_count) > _size)
            {
                if (RunGC()) // perform GC to discover holes
                {
                    return Create(init); // return from a hole
                }
                else // GC discovered no holes
                {
                    // we need to allocate a new array
                    _size++;
                    _pools[_size-3] = new TObject[1 << _size];
                }
            }

            var obj = _count;
            _bitmap.Set(obj, true);
            _count++;

            if (init)
            {
                ref TObject instance = ref Get(hole);
                instance = new TObject();
            }

            return obj;
        }

        /// <summary>
        /// Retrieves a reference to the object in the pool given a valid index.
        /// </summary>
        public ref TObject Get(uint index)
        {
            if (!_bitmap.Get(index))
                throw new InvalidOperationException("Cannot access an allocated object from the pool.");

            if (index < 16)
            {
                return ref _pools[0][index];
            }

            var poolIdx = BitOperations.Log2(index);
            index -= (uint)(1 << poolIdx);

            return ref _pools[poolIdx-3][index];
        }

        /// <summary>
        /// Frees the object at specified index to be reused later.
        /// </summary>
        public void Free(uint index) => _bitmap.Set(index, false);

        /// <summary>
        /// Checks if object at <paramref name="index"/> can be retrieved.
        /// </summary>
        public bool AllocatedAt(uint index) => _bitmap.Get(index);

        /// <summary>
        /// Enumerates all elements of the bitmap to find freed objects
        /// and pushes their indexes on the _holes stack to be used in <see cref="Create"/>.
        /// </summary>
        private bool RunGC()
        {
            bool foundSpace = false;
            foreach (var hole in _bitmap.EnumerateFalse())
            {
                foundSpace = true;
                _holes.Push(hole);
            }
            return foundSpace;
        }
    }
}