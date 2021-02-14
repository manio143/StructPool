using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace Pool
{
    public class Bitmap
    {
        private BitArray _array;
        public Bitmap(int capacity)
        {
            _array = new BitArray(capacity);
        }

        public bool Get(uint index)
        {
            if(_array.Count <= index)
            {
                _array.Length = 1 << (BitOperations.Log2(index) + 1);
            }
            return _array[(int)index];
        }

        public void Set(uint index, bool value)
        {
            if(_array.Count <= index)
            {
                _array.Length = 1 << (BitOperations.Log2(index) + 1);
            }
            _array.Set((int)index, value);
        }

        public IEnumerable<uint> EnumerateFalse()
        {
            uint idx = 0;
            foreach(bool element in _array)
            {
                if (!element)
                    yield return idx;
                idx++;
            }
        }
    }
}