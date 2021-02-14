using System;

namespace Pool
{
    class Program
    {
        static void Main(string[] args)
        {
            var o1 = FastPool<A>.Default.Create();
            var o2 = FastPool<A>.Default.Create(true);

            ref var obj1 = ref FastPool<A>.Default.Get(o1);
            ref var obj2 = ref FastPool<A>.Default.Get(o2);

            obj1.X = 10;
            obj2.X = 5;

            FastPool<A>.Default.Free(o1);
            FastPool<A>.Default.Free(o2);
            // Note: there's no way to prevent use after free

            for (int i = 0; i < 20; i++)
            {
                var o = FastPool<A>.Default.Create();
                ref var obj = ref FastPool<A>.Default.Get(o);
                if (obj.X > 0)
                    Console.WriteLine("Reused obj({1}): {0}", obj.X, o);
                obj.X = i;
                Console.WriteLine("Setting obj{1}: {0}", obj.X, o);
            }
        }
    }

    struct A
    {
        public int X { get; set; }
    }
}
