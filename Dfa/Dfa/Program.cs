using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DfaMuie
{
    public enum AcceptorResult
    {
        Rejected,
        Accepted,
        Aborted
    }

    public abstract class Char
    {
    }

    public class Lambda : Char
    {
    }

    public class Letter : Char
    {
        public Letter(string id)
        {
            Id = id;
        }

        public string Id;
    }

    public class Link
    {
        public Link(Char label, Node from, Node to)
        {
            Label = label;
            From = from;
            To = to;
        }

        public Char Label;
        public Node From, To;
    };

    public class Node
    {
        public Node(string id, bool isFinalState = false)
        {
            Id = id;
            IsFinalState = isFinalState;
        }

        public string Id;
        public bool IsFinalState;
        public List<Link> Edges = new List<Link>();
    };

    public class DFA
    {
        private readonly Dictionary<Node, List<Link>> _graph = new Dictionary<Node, List<Link>>();
        private readonly List<Node> _initialState;
        private List<Node> _states;

        public DFA(IEnumerable<Node> nodes, IEnumerable<Link> links, Node startNode)
        {
            foreach (var node in nodes)
            {
                if (!_graph.ContainsKey(node))
                    _graph.Add(node, new List<Link>());
            }
            foreach (var link in links)
            {
                _graph[link.From].Add(link);
            }
            _initialState = new List<Node> { startNode };
            Reset();
        }

        public void Reset()
        {
            _states = _initialState;
        }

        public void PrintCurrentStates()
        {
            foreach (var state in _states)
            {
                Console.Write(state.Id + " ");
            }
            Console.WriteLine("");
        }

        public void Advance(Letter c)
        {
            /*_states = (from state in _states
                from link in _graph[state].Where(link => (link.Label is Letter) && (((Letter) link.Label).Id == c.Id))
                select link.To).ToList();*/
            var _newStates = new List<Node>();
            foreach (var state in _states)
            {
                _newStates.AddRange(
                    (from link in _graph[state]
                     where (link.Label is Letter) && (((Letter)link.Label).Id == c.Id)
                     select link.To).ToList());
            }
            _states = _newStates;
            //PrintCurrentStates();
        }

        public AcceptorResult CurrentState()
        {
            if (_states.Count == 0) return AcceptorResult.Aborted;
            return _states.Any(t => t.IsFinalState) ? AcceptorResult.Accepted : AcceptorResult.Rejected;
        }

        public AcceptorResult ServeInput(IEnumerable<Letter> input)
        {
            foreach (var t in input)
                Advance(t);
            return CurrentState();
        }
    };

    public class AcceptorBuilder
    {
        private const string LambdaId = "$";
        private const string StartStateId = "0";

        private static readonly Dictionary<string, Node> _nodeMap = new Dictionary<string, Node>();
        private static readonly Dictionary<string, Char> _alphabetMap = new Dictionary<string, Char>();

        private static IEnumerable<Node> Nodes => _nodeMap.Values.ToList();
        private static readonly List<Link> Links = new List<Link>();
        private static Node _startNode;

        public static void ParseCreationInput(StreamReader streamReader)
        {
            int counter = 0;
            string line;
            while ((line = streamReader.ReadLine()) != null)
            {
                Console.WriteLine(line);
                var elems = line.Split(' ');
                if (counter == 0)
                {
                    foreach (var t in elems)
                    {
                        if (_nodeMap.ContainsKey(t))
                            _nodeMap[t].IsFinalState = true;
                        else
                            _nodeMap.Add(t, new Node(t, true));
                    }
                }
                else
                {
                    string from = elems[0], link = elems[1], to = elems[2];

                    if (!_nodeMap.ContainsKey(from))
                        _nodeMap.Add(from, new Node(from));
                    if (!_nodeMap.ContainsKey(to))
                        _nodeMap.Add(to, new Node(to));
                    if (!_alphabetMap.ContainsKey(link))
                    {
                        Char label;
                        if (elems[1] == LambdaId)
                            label = new Lambda();
                        else label = new Letter(link);
                        _alphabetMap.Add(link, label);
                    }

                    Links.Add(new Link(_alphabetMap[link], _nodeMap[from], _nodeMap[to]));
                }
                counter++;
            }
            if (!_nodeMap.ContainsKey(StartStateId))
                _nodeMap.Add(StartStateId, new Node(StartStateId));
            _startNode = _nodeMap[StartStateId];
        }

        public static DFA GetDfa()
        {
            return new DFA(Nodes, Links, _startNode);
        }

        public static IEnumerable<Letter> ParseTestInput(string input)
        {
            var answerLetters = new List<Letter>();

            foreach (var letter in input.ToCharArray().ToList().Select(e => e.ToString()))
            {
                if (!_alphabetMap.ContainsKey(letter))
                    _alphabetMap.Add(letter, new Letter(letter));
                if (_alphabetMap[letter] is Letter)
                    answerLetters.Add((Letter)_alphabetMap[letter]);
            }
            return answerLetters;
        }
    }

    internal class Program
    {


        private static StreamReader OpenFile(string fileName)
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fileName);
            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            return new StreamReader(fileStream, Encoding.UTF8);
        }

        private static void RunTests(DFA dfa)
        {
            var input = Console.ReadLine();
            while (true)
            {
                var letters = AcceptorBuilder.ParseTestInput(input);
                var ans = dfa.ServeInput(letters);
                Console.WriteLine(ans.ToString());

                dfa.Reset();
                input = Console.ReadLine();
            }
        }

        private static void Main(string[] args)
        {
            using (var streamReader = OpenFile(@"Input.txt"))
                AcceptorBuilder.ParseCreationInput(streamReader);

            var dfa = AcceptorBuilder.GetDfa();

            RunTests(dfa);
        }
    }
}
