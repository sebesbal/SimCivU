using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
//using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace SimcivEngine
{
    namespace Graph
    {
        /// <summary>
        /// 
        /// </summary>
        internal class Node<N, E> where N : Node<N, E> where E : Edge<N, E>
        {
            public int id;
            public List<E> edges = new List<E>();
            E EdgeTo(N n)
            {
                foreach (var e in edges)
                {
                    if (e.src == n)
                    {
                        return e;
                    }
                }
                return null;
            }
        }

        internal class Edge<N, E> where N : Node<N, E> where E : Edge<N, E>
        {
            double cost;
            public N src;
            public N dst;
        }

        internal class IntNode : Node<IntNode, IntEdge>
        {
            int dummy;
        }

        internal class IntEdge : Edge<IntNode, IntEdge>
        { }
        /// <summary>
        /// Represents the graph of the map
        ///     Path finding
        ///     Path cost
        /// </summary>
        internal class Grid<N, E> where N: Node<N, E> where E: Edge<N, E>
        {
            public List<N> nodes = new List<N>();
            public void Builder(GridBuilder<N, E> builder) { }
        }

        /// <summary>
        /// Creates Nodes and Edges for a Grid
        /// </summary>
        class GridBuilder<N, E>
        { }

        internal class GridBuilderHexa<N, E> : GridBuilder<N, E>
        { }

        internal class GridBuilderIsomorh<N, E> : GridBuilder<N, E>
        {

        }
    }
}