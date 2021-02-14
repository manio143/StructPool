using System;
using System.Collections.Generic;
using System.Numerics;

namespace Pool
{
    public class FastPool<TObject> where TObject : struct
    {
        private TObject[][] _pools = new TObject[12][];
        private Bitmap _bitmap;
        private uint _count;
        private ushort _size;
        private Stack<uint> _holes = new Stack<uint>(16);

        public static FastPool<TObject> Default = new FastPool<TObject>();

        public FastPool() : this(0) {}

        public FastPool(uint capacity)
        {
            // minimum capacity is 15
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

        public void Free(uint index) => _bitmap.Set(index, false);

        public bool AllocatedAt(uint index) => _bitmap.Get(index);

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