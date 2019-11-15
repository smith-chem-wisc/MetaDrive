using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassSpectrometry
{
    public class Vertex<T>
    {
        public Vertex(T value, IEnumerable<Vertex<T>> neighbors = null)
        {
            Value = value;
            Neighbors = neighbors?.ToList() ?? new List<Vertex<T>>();
        }

        public T Value { get; }

        public List<Vertex<T>> Neighbors { get; }

        public double Score { get; set; }

        public void AddEdge(Vertex<T> vertex)
        {
            Neighbors.Add(vertex);
        }

        public Vertex<T> BestScoreNeighbor { get; set; }
        public double UpScore { get; set; }

    }

    public class Graph<T>
    {
        public Graph(IEnumerable<Vertex<T>> initialNodes = null)
        {
            Vertices = initialNodes?.ToList() ?? new List<Vertex<T>>();
        }


        public List<Vertex<T>> Vertices { get; }

        public void AddToList(Vertex<T> vertex)
        {
            if (!Vertices.Contains(vertex))
            {
                Vertices.Add(vertex);
            }
        }
    }

}
