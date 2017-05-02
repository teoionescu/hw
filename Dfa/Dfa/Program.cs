using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;

namespace Dfa
{
    public static class Utils
    {
        public const string ConstLambdaId = "_";
        public const string ConstStartStateId = "0";
        public static StreamReader GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return new StreamReader(stream);
        }
        public static IEnumerable<IEnumerable<T>> Subsets<T>(IEnumerable<T> source)
        {
            List<T> list = source.ToList();
            int length = list.Count;
            int max = (int)Math.Pow(2, list.Count);

            for (int count = 0; count < max; count++)
            {
                List<T> subset = new List<T>();
                uint rs = 0;
                while (rs < length)
                {
                    if ((count & (1u << (int)rs)) > 0)
                    {
                        subset.Add(list[(int)rs]);
                    }
                    rs++;
                }
                yield return subset;
            }
        }
    }

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
    }

    public class Node
    {
        public Node(string id, bool isFinalState = false)
        {
            Id = id;
            IsFinalState = isFinalState;
        }
        public Node(Node from)
        {
            this.Id = from.Id;
            this.IsFinalState = from.IsFinalState;
        }

        public string Id;
        public bool IsFinalState;
    }

    public class Definition
    {
        public List<Node> Nodes = new List<Node>();
        public List<Link> Links = new List<Link>();
        public Node StartNode;

        public string AsString()
        {
            var s = Nodes.Where(node => node.IsFinalState).Aggregate("", (current, node) => current + node.Id + " ");
            s = s.Substring(0, s.Length - 1) + "\n";
            foreach (var link in Links)
            {
                s += link.From.Id + " ";
                if (link.Label is Lambda) s += Utils.ConstLambdaId + " ";
                else s += ((Letter)link.Label).Id + " ";
                s += link.To.Id + "\n";
            }
            return s;
        }
    }

    public abstract class FA
    {
        protected readonly Dictionary<Node, List<Link>> Graph = new Dictionary<Node, List<Link>>();
        protected readonly List<Node> InitialState;
        protected List<Node> States;

        protected FA(Definition definition)
        {
            foreach (var node in definition.Nodes)
            {
                if (!Graph.ContainsKey(node))
                    Graph.Add(node, new List<Link>());
            }
            foreach (var link in definition.Links)
            {
                Graph[link.From].Add(link);
            }
            InitialState = new List<Node> { definition.StartNode };
            Reset();
        }

        public virtual void Reset(IEnumerable<Node> newStates = null)
        {
            States = newStates?.ToList() ?? InitialState;
        }
        protected void PrintCurrentStates()
        {
            Console.Write("{ ");
            foreach (var state in States)
            {
                Console.Write(state.Id + " ");
            }
            Console.WriteLine("}");
        }
        public List<Node> CurrentStatesList()
        {
            return States;
        }
        public abstract List<Node> Advance(Letter c);
        public AcceptorResult CurrentState()
        {
            if (States.Count == 0) return AcceptorResult.Aborted;
            return States.Any(t => t.IsFinalState) ? AcceptorResult.Accepted : AcceptorResult.Rejected;
        }
        public AcceptorResult ServeInput(IEnumerable<Letter> input)
        {
            foreach (var t in input)
                Advance(t);
            return CurrentState();
        }
    }

    public class LNFA : FA
    {
        public LNFA(Definition definition)
            : base(definition)
        {
        }
        public List<Node> ExpandOnLnfa(List<Node> startStates)
        {
            var expandStates = new List<Node>(startStates);
            var initialCount = -1;
            do
            {
                initialCount = expandStates.Count;
                var newlyAdded = new List<Node>();
                foreach (var state in expandStates)
                {
                    newlyAdded.AddRange(
                        (from link in Graph[state]
                            where link.Label is Lambda
                            select link.To).ToList());
                }
                expandStates.AddRange(newlyAdded.Distinct());
                expandStates = expandStates.Distinct().ToList();
            } while (initialCount != expandStates.Count);

            return expandStates.Distinct().ToList();
        }
        public override List<Node> Advance(Letter c)
        {
            States = ExpandOnLnfa(States.Distinct().ToList());

            var newStates = new List<Node>();
            foreach (var state in States)
            {
                newStates.AddRange(
                    (from link in Graph[state]
                        where link.Label is Letter && ((Letter)link.Label).Id == c.Id
                        select link.To).ToList());
            }

            States = ExpandOnLnfa(newStates.Distinct().ToList());
            return States;
        }
    }

    public class NFA : FA
    {
        public NFA(Definition definition) : base(definition)
        {
        }
        public override List<Node> Advance(Letter c)
        {
            var newStates = new List<Node>();
            foreach (var state in States)
            {
                newStates.AddRange(
                    (from link in Graph[state]
                        where link.Label is Letter && ((Letter)link.Label).Id == c.Id
                        select link.To).ToList());
            }
            States = newStates.Distinct().ToList();
            return States;
        }
    }

    public class DFA : FA
    {
        public DFA(Definition definition) : base(definition)
        {
        }
        public override List<Node> Advance(Letter c)
        {
            var singularState = States.Count>0 ? States[0] : null;
            if (singularState != null)
            {
                singularState =
                (from link in Graph[singularState]
                    where link.Label is Letter && ((Letter)link.Label).Id == c.Id
                    select link.To).FirstOrDefault();
            }
            States = singularState != null ? new List<Node> { singularState } : new List<Node>() { };
            return States;
        }
    }

    public class Acceptor
    {
        private static readonly Dictionary<string, Node> NodeMap = new Dictionary<string, Node>();
        private static readonly Dictionary<string, Char> AlphabetMap = new Dictionary<string, Char>();
        private static readonly HashSet<string> SeenLines = new HashSet<string>();
        public static Definition ParseDefinition(string input, string labdaId, string startStateId)
        {
            NodeMap.Clear();
            AlphabetMap.Clear();
            SeenLines.Clear();
            var definition = new Definition();
            using (var streamReader = Utils.GenerateStreamFromString(input))
            {
                var counter = 0;
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    var elems = line.Split(' ');
                    if (counter == 0)
                    {
                        foreach (var t in elems)
                        {
                            if (NodeMap.ContainsKey(t))
                                NodeMap[t].IsFinalState = true;
                            else
                                NodeMap.Add(t, new Node(t, true));
                        }
                    }
                    else
                    {
                        string from = elems[0], link = elems[1], to = elems[2];
                        var hs = from + link + to;
                        if (SeenLines.Contains(hs)) continue;
                        SeenLines.Add(hs);

                        if (!NodeMap.ContainsKey(from))
                            NodeMap.Add(from, new Node(from));
                        if (!NodeMap.ContainsKey(to))
                            NodeMap.Add(to, new Node(to));
                        if (!AlphabetMap.ContainsKey(link))
                        {
                            Char label;
                            if (elems[1] == labdaId)
                                label = new Lambda();
                            else label = new Letter(link);
                            AlphabetMap.Add(link, label);
                        }

                        definition.Links.Add(new Link(AlphabetMap[link], NodeMap[from], NodeMap[to]));
                    }
                    counter++;
                }
                if (!NodeMap.ContainsKey(startStateId))
                    NodeMap.Add(startStateId, new Node(startStateId));
                definition.StartNode = NodeMap[startStateId];
                definition.Nodes = NodeMap.Values.ToList();
            }
            return definition;
        }

        public static Definition LnfaToNfa(Definition definition)
        {
            // because I use alphabet
            ParseDefinition(definition.AsString(), Utils.ConstLambdaId, Utils.ConstStartStateId);
            var newLinks = new List<Link>();
            foreach (var node in definition.Nodes)
            {
                var def = new Definition
                {
                    Nodes = definition.Nodes,
                    Links = definition.Links,
                    StartNode = node
                };
                var lnfa = new LNFA(def);
                foreach (var chr in AlphabetMap.Values)
                {
                    if (chr is Letter)
                    {
                        var lett = (Letter)chr;

                        var finalCurrent = lnfa.Advance(lett);
                        foreach (var fin in finalCurrent)
                        {
                            newLinks.Add(new Link(lett, node, fin));
                        }
                        lnfa.Reset();
                    }
                }
                foreach (var fin in lnfa.ExpandOnLnfa(new List<Node> { node }))
                {
                    if (fin.IsFinalState) node.IsFinalState = true;
                }
            }
            return new Definition
            {
                Nodes = definition.Nodes,
                Links = newLinks,
                StartNode = definition.StartNode
            };
        }

        public static Definition NfaToDfa(Definition definition)
        {
            // because I use alphabet
            ParseDefinition(definition.AsString(), Utils.ConstLambdaId, Utils.ConstStartStateId);
            var prototypeNfa = new NFA(definition);
            var newFinalStates = "";
            var newLinks = "";
            foreach (var subsetEnum in Utils.Subsets(definition.Nodes))
            {
                var subset = subsetEnum.ToList();
                subset.Sort((n1, n2) => string.Compare(n1.Id, n2.Id));
                if (!subset.Any()) continue;
                var subsetId = subset.Aggregate("", (current, t) => current + t.Id);
                var subsetState = subset.Any(t => t.IsFinalState);
                if (subsetState) newFinalStates += subsetId + " ";
                foreach (var chr in AlphabetMap.Values)
                {
                    if (!(chr is Letter)) continue;
                    var lett = (Letter)chr;
                    prototypeNfa.Reset(subset);
                    prototypeNfa.Advance(lett);
                    var toSubset = prototypeNfa.CurrentStatesList();
                    if (!toSubset.Any()) continue;
                    toSubset.Sort((n1, n2) => string.Compare(n1.Id, n2.Id));
                    var toSubsetId = toSubset.Aggregate("", (current, t) => current + t.Id);
                    newLinks += subsetId + " " + lett.Id + " " + toSubsetId + "\n";
                }
            }
            newFinalStates = newFinalStates.Substring(0, newFinalStates.Length - 1) + "\n";
            return ParseDefinition((newFinalStates + newLinks), Utils.ConstLambdaId, Utils.ConstStartStateId);
        }

        public static Definition DfaMergeEquivalent(Definition definition)
        {
            // because I use alphabet
            ParseDefinition(definition.AsString(), Utils.ConstLambdaId, Utils.ConstStartStateId);
            var prototypeDfa = new DFA(definition);
            var table = new Dictionary<Tuple<Node, Node>, bool>();
            for (var i = 0; i < definition.Nodes.Count; i++)
            for (var j = 0; j < definition.Nodes.Count; j++)
                table.Add(new Tuple<Node, Node>(definition.Nodes[i], definition.Nodes[j]), false);
            var totalElems = table.Keys.ToList();
            foreach (var elem in totalElems)
                if (elem.Item1.IsFinalState ^ elem.Item2.IsFinalState) table[elem] = true;
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var elem in totalElems)
                {
                    if (table[elem]) continue;
                    foreach (var chr in AlphabetMap.Values)
                    {
                        if (!(chr is Letter)) continue;
                        var lett = (Letter) chr;
                        Node t1 = null, t2 = null;
                        prototypeDfa.Reset(new List<Node> {elem.Item1});
                        prototypeDfa.Advance(lett);
                        var lst = prototypeDfa.CurrentStatesList();
                        if (lst.Count > 0) t1 = lst[0];
                        prototypeDfa.Reset(new List<Node> {elem.Item2});
                        prototypeDfa.Advance(lett);
                        lst = prototypeDfa.CurrentStatesList();
                        if (lst.Count > 0) t2 = lst[0];
                        if (t1 != null && t2 != null)
                        {
                            if (!table[new Tuple<Node, Node>(t1, t2)]) continue;
                            table[elem] = true;
                            changed = true;
                        }
                        else if (!(t1 == null && t2 == null))
                        {
                            table[elem] = true;
                            changed = true;
                        }
                    }
                }
            }
            var graph = definition.Nodes.ToDictionary(node => node, node => new List<Node>());
            var mk = definition.Nodes.ToDictionary(node => node, node => false);
            var idx = definition.Nodes.ToDictionary(node => node, node => "");
            foreach (var elem in totalElems)
            {
                if (table[elem]) continue;
                graph[elem.Item1].Add(elem.Item2);
                graph[elem.Item2].Add(elem.Item1);
            }
            var newFinalStates = "";
            var newLinks = "";
            foreach (var node in definition.Nodes)
            {
                if (mk[node]) continue;
                var uniList = new List<Node>();
                var q = new Queue<Node>();
                mk[node] = true;
                q.Enqueue(node);
                while (q.Count != 0)
                {
                    var nd = q.Dequeue();
                    uniList.Add(nd);
                    foreach (var it in graph[nd].Where(it => !mk[it]))
                    {
                        mk[it] = true;
                        q.Enqueue(it);
                    }
                }
                uniList.Sort((n1, n2) => string.Compare(n1.Id, n2.Id));
                var subsetId = uniList.Aggregate("", (current, t) => current + t.Id);
                var subsetState = uniList.Any(t => t.IsFinalState);
                if (subsetState) newFinalStates += subsetId + " ";
                foreach (var nd in uniList) idx[nd] = subsetId;
            }
            foreach (var link in definition.Links)
            {
                if (link.Label is Lambda) throw new Exception("this is not dfa?");
                newLinks += idx[link.From] + " " + ((Letter)link.Label).Id + " " + idx[link.To] + "\n";
            }
            newFinalStates = newFinalStates.Substring(0, newFinalStates.Length - 1) + "\n";
            return ParseDefinition((newFinalStates + newLinks), Utils.ConstLambdaId,
                idx[definition.Nodes.First(node => node.Id == Utils.ConstStartStateId)]);
        }

        public static Definition DfaRemoveUnreachable(Definition definition)
        {
            var graph = definition.Nodes.ToDictionary(node => node, node => new List<Node>());
            var mk = definition.Nodes.ToDictionary(node => node, node => false);
            foreach (var link in definition.Links)
                graph[link.From].Add(link.To);
            var q = new Queue<Node>();
            mk[definition.StartNode] = true;
            q.Enqueue(definition.StartNode);
            while (q.Count != 0)
            {
                var nd = q.Dequeue();
                foreach (var it in graph[nd].Where(it => !mk[it]))
                {
                    mk[it] = true;
                    q.Enqueue(it);
                }
            }
            return new Definition
            {
                StartNode = definition.StartNode,
                Nodes = definition.Nodes.Where(node => mk[node]).ToList(),
                Links = definition.Links.Where(link => mk[link.From] && mk[link.To]).ToList()
            };
        }

        public static Definition DfaRemoveDeadEnds(Definition definition)
        {
            var graph = definition.Nodes.ToDictionary(node => node, node => new List<Node>());
            var mk = definition.Nodes.ToDictionary(node => node, node => false);
            foreach (var link in definition.Links)
                graph[link.To].Add(link.From);
            var q = new Queue<Node>();
            foreach (var node in definition.Nodes)
                if (node.IsFinalState)
                {
                    mk[node] = true;
                    q.Enqueue(node);
                }
            while (q.Count != 0)
            {
                var nd = q.Dequeue();
                foreach (var it in graph[nd].Where(it => !mk[it]))
                {
                    mk[it] = true;
                    q.Enqueue(it);
                }
            }
            return new Definition
            {
                StartNode = definition.StartNode,
                Nodes = definition.Nodes.Where(node => mk[node]).ToList(),
                Links = definition.Links.Where(link => mk[link.From] && mk[link.To]).ToList()
            };
        }

        public static Definition RemoveRedundant(Definition definition)
        {
            // because I use alphabet
            ParseDefinition(definition.AsString(), Utils.ConstLambdaId, Utils.ConstStartStateId);

            var translate = new Dictionary<Node, string>();

            foreach (var node in definition.Nodes)
            {
                var newId = "";
                foreach (var chr in AlphabetMap.Values)
                {
                    if (!(chr is Letter)) continue;
                    var lett = (Letter)chr;
                    newId += "*" + lett.Id;
                    var reach = new LNFA(new Definition
                    {
                        Nodes = definition.Nodes,
                        Links = definition.Links,
                        StartNode = node
                    }).Advance(lett);
                    reach.Sort((n1, n2) => string.Compare(n1.Id, n2.Id));
                    newId = reach.Aggregate(newId, (current, t) => current + ("-" + t.Id));
                }
                if (translate.ContainsKey(node)) throw new Exception("no duplicate Nodes pls");
                newId += "&" + node.IsFinalState;
                translate.Add(node, newId);
            }
            foreach (var node in definition.Nodes)
                node.Id = translate[node];

            var reduce = ParseDefinition(definition.AsString(), Utils.ConstLambdaId, definition.StartNode.Id);

            var idx = 1;
            foreach (var node in reduce.Nodes)
                if (node.Id != reduce.StartNode.Id)
                {
                    node.Id = idx.ToString();
                    idx++;
                }
            reduce.StartNode.Id = Utils.ConstStartStateId;
            return reduce;
        }

        public static IEnumerable<Letter> ParseTestInput(string input)
        {
            var answerLetters = new List<Letter>();

            foreach (var letter in input.ToCharArray().ToList().Select(e => e.ToString()))
            {
                if (!AlphabetMap.ContainsKey(letter))
                    AlphabetMap.Add(letter, new Letter(letter));
                if (AlphabetMap[letter] is Letter)
                    answerLetters.Add((Letter)AlphabetMap[letter]);
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

        private static void RunTests(FA fa)
        {
            var input = Console.ReadLine();
            while (true)
            {
                var letters = Acceptor.ParseTestInput(input);
                var ans = fa.ServeInput(letters);
                Console.WriteLine(ans.ToString());

                fa.Reset();
                input = Console.ReadLine();
            }
        }

        private static void TEMA_LNFA_TO_NFA(string definitionInput)
        {
            var lnfaDef = Acceptor.ParseDefinition(definitionInput, Utils.ConstLambdaId, Utils.ConstStartStateId);
            var nfaDef = Acceptor.LnfaToNfa(lnfaDef);
            Console.WriteLine("LNFA citit:");
            Console.Write(lnfaDef.AsString());
            Console.WriteLine("*************************");
            Console.WriteLine("NFA rezultat:");
            Console.Write(nfaDef.AsString());
            Console.WriteLine("*************************");
            var reducedDef = Acceptor.RemoveRedundant(nfaDef);
            Console.WriteLine("NFA redus:");
            Console.Write(reducedDef.AsString());
            Console.WriteLine("*************************");

            RunTests(new NFA(reducedDef));
        }

        private static void TEMA_NFA_TO_DFA(string definitionInput)
        {
            var nfaDef = Acceptor.ParseDefinition(definitionInput, Utils.ConstLambdaId, Utils.ConstStartStateId);
            Console.WriteLine("NFA citit:");
            Console.Write(nfaDef.AsString());
            Console.WriteLine("*************************");

            var dfaDef = Acceptor.NfaToDfa(nfaDef);

            Console.WriteLine("DFA rezultat:");
            Console.Write(dfaDef.AsString());
            Console.WriteLine("*************************");

            var reducedDef = Acceptor.RemoveRedundant(dfaDef);

            Console.WriteLine("DFA redus:");
            Console.Write(reducedDef.AsString());
            Console.WriteLine("*************************");

            RunTests(new DFA(reducedDef));
        }

        private static void TEMA_MIN_DFA(string definitionInput)
        {
            var dfamin = Acceptor.ParseDefinition(definitionInput, Utils.ConstLambdaId, Utils.ConstStartStateId);
            Console.WriteLine("DFA citit:");
            Console.Write(dfamin.AsString());
            Console.WriteLine("*************************");

            dfamin = Acceptor.DfaRemoveUnreachable(dfamin);

            Console.WriteLine("DFA no unreachable:");
            Console.Write(dfamin.AsString());
            Console.WriteLine("*************************");

            dfamin = Acceptor.DfaRemoveDeadEnds(dfamin);

            Console.WriteLine("DFA no dead-ends:");
            Console.Write(dfamin.AsString());
            Console.WriteLine("*************************");

            dfamin = Acceptor.DfaMergeEquivalent(dfamin);

            Console.WriteLine("DFA merge equivalent:");
            Console.Write(dfamin.AsString());
            Console.WriteLine("*************************");

            dfamin = Acceptor.RemoveRedundant(dfamin);

            Console.WriteLine("DFA redus:");
            Console.Write(dfamin.AsString());
            Console.WriteLine("*************************");

            RunTests(new DFA(dfamin));
        }

        private static void Main()
        {
            string definitionInput;
            using (var streamReader = OpenFile(@"InputDefinition.txt"))
                definitionInput = streamReader.ReadToEnd();
            //TEMA_LNFA_TO_NFA(definitionInput);
            //TEMA_NFA_TO_DFA(definitionInput);
            TEMA_MIN_DFA(definitionInput);
        }
    }
}