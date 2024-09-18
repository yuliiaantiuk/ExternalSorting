using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace External_Sort
{
    class MinHeapNode
    {
        public int Element { get; set; }
        public int Index { get; set; }

        public MinHeapNode(int element, int index)
        {
            Element = element;
            Index = index;
        }
    }
}
