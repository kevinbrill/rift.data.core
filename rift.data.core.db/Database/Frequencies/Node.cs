using System;

namespace Assets.Database.Frequencies
{
	public class Node : IComparable
	{
		public int Frequency { get; set; }
		public int Value { get; set; }
		public int Depth { get; set; }

		public Node Parent { get; set; }
		public Node A { get; set; }
		public Node B { get; set; }

		public Node()
		{

		}

		public Node(Node a, Node b) : this()
		{
			Frequency = a.Frequency + b.Frequency;
			Value = -1;

			A = a;
			A.Parent = this;

			B = b;
			B.Parent = this;

			Depth = Math.Min(A.Depth, B.Depth) + 1;
		}

		public bool IsNode()
		{
			return A != null;
		}

		public bool IsLeaf()
		{
			return A == null;
		}

		public int CompareTo(object objectNode)
		{
			if (!(objectNode is Node)) throw new ArgumentOutOfRangeException();

			Node node = (Node)objectNode;
			int d = Frequency - node.Frequency;

			if (d == 0)
			{
				// Nodes always after
				if (IsNode() && node.IsLeaf())
				{
					return +1;
				}

				if (IsLeaf() && node.IsNode())
				{
					return -1;
				}
			}

			return d;
		}
	}
}
